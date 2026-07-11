import tensorstack.utils as Utils
import tensorstack.data_objects as DataObjects
import tensorstack.quantization as Quantization
from tensorstack.enums import ProcessType, QuantTarget
Utils.redirect_output()
Utils.create_services()

import torch
import threading
import numpy as np
from pathlib import Path
from threading import Event
from functools import partial
from collections.abc import Buffer
from typing import Dict, Sequence, List, Tuple, Optional, Any
from transformers import (
    AutoTokenizer,
    AutoModelForCausalLM,
    TextIteratorStreamer,
    PreTrainedModel,
    PreTrainedTokenizerBase
)

from dataclasses import dataclass
@dataclass(slots=True)
class TextPipeline:
    tokenizer: PreTrainedTokenizerBase
    transformer: PreTrainedModel
    streamer: TextIteratorStreamer
    kwargs: dict[str, Any]


# Globals
_config = None
_model_config = None
_pipeline = None
_processType = None
_execution_device = None
_device_map = None
_pipeline_device_map = None
_generator = None
_isMemoryOffload = False
_prompt_cache_key = None
_prompt_cache_value = None
_cancel_event = Event()
_stopwatch = None


#------------------------------------------------
# Load Pipeline
#------------------------------------------------
def load(config_args: Dict[str, Any]) -> bool:
    global _config, _pipeline, _generator, _processType, _execution_device, _isMemoryOffload

    # Config
    _config = DataObjects.PipelineConfig(**config_args)
    _execution_device = Utils.get_execution_device(_config)
    _generator = torch.Generator(device=_execution_device)
    _processType = _config.process_type

    # Initialize Pipeline
    _pipeline = initialize(_config)

    # Memory
    #_isMemoryOffload = Utils.configure_pipeline_memory(_pipeline, _execution_device, _config)
    Utils.trim_memory(_isMemoryOffload)
    return True


#------------------------------------------------
# Reload Pipeline - ProcessType, LoraAdapters and ControlNet are the only options that can be modified
#------------------------------------------------
def reload(config_args: Dict[str, Any]) -> bool:
    global _config, _pipeline, _processType

    # Config
    _config = DataObjects.PipelineConfig(**config_args)
    _processType = _config.process_type

    # Rebuild Pipeline
    _pipeline = create_pipeline(_config)

    # Memory
    #Utils.configure_pipeline_memory(_pipeline, _execution_device, _config)
    Utils.trim_memory(_isMemoryOffload)
    return True


#------------------------------------------------
# Switch Pipeline - ProcessType
#------------------------------------------------
def switch(process_type: ProcessType) -> bool:
    global _pipeline, _processType

    # Switch Pipeline
    current = _processType
    _processType = process_type
    _pipeline = create_pipeline(_config)

    print(f"[Generate] Switched pipeline: {current} => {process_type}")
    return True


#------------------------------------------------
# Cancel Generation
#------------------------------------------------
def generateCancel() -> None:
    _cancel_event.set()


#------------------------------------------------
# Unload Pipline
#------------------------------------------------
def unload() -> bool:
    global _pipeline, _prompt_cache_key, _prompt_cache_value
    _pipeline = None
    _prompt_cache_key = None
    _prompt_cache_value = None
    Utils.trim_memory(_isMemoryOffload)
    return True


#------------------------------------------------
# Get the notifications
#------------------------------------------------
def getNotifications() -> list[(str, Buffer)]:
    return Utils.notification_get()


#------------------------------------------------
# Get the log entires
#------------------------------------------------
def getLogs() -> list[str]:
    return Utils.get_output()


#------------------------------------------------
# Initialize Pipeline
#------------------------------------------------
def initialize(config: DataObjects.PipelineConfig):
    global _model_config, _device_map, _pipeline_device_map

    _device_map = Utils.get_device_map(config, _execution_device)
    _pipeline_device_map = Utils.get_pipeline_device_map(config, _execution_device)
    _model_config = Utils.get_model_config(__file__, config)
    return create_pipeline(config)


#------------------------------------------------
# Load AutoTokenizer
#------------------------------------------------
def load_tokenizer(config: DataObjects.PipelineConfig, pipeline_kwargs: Dict[str, str]):
    if _pipeline and _pipeline.tokenizer:
        print(f"[Load] Loading Cached Tokenizer")
        return _pipeline.tokenizer

    tokenizer_path: Path = _model_config["transformer"]
    tokenizer_config: Path = _model_config["transformer_config"]

    # 1. Load from pretrained folder
    print(f"[Load] Loading Pretrained Tokenizer")
    tokenizer = AutoTokenizer.from_pretrained(
        tokenizer_path,
        #config=tokenizer_config,
        dtype=config.data_type,
        **pipeline_kwargs
    )
    return tokenizer


#------------------------------------------------
# Load AutoModelForCausalLM
#------------------------------------------------
def load_transformer(config: DataObjects.PipelineConfig, pipeline_kwargs: Dict[str, str]):
    if _pipeline and _pipeline.transformer:
        print(f"[Load] Loading Cached Transformer")
        return _pipeline.transformer

    transformer_path: Path = _model_config["transformer"]
    transformer_config: Path = _model_config["transformer_config"]

    # 1. Load from pretrained folder
    print(f"[Load] Loading Pretrained Transformer")
    transformer = AutoModelForCausalLM.from_pretrained(
        transformer_path,
        #config=transformer_config,
        dtype=config.data_type,
        device_map=_device_map,
        quantization_config=Quantization.auto_pretrained_config(config, QuantTarget.TEXT_ENCODER),
        **pipeline_kwargs
    )
    Utils.trim_memory(True)
    return transformer


#------------------------------------------------
# Create a new pipeline
#------------------------------------------------
def create_pipeline(config: DataObjects.PipelineConfig):
    template_path: Path = _model_config["template"]
    pipeline_kwargs = {
        "variant": config.variant,
        "use_safetensors":True,
        "low_cpu_mem_usage":True,
        "local_files_only":True,
    }

    # Load Models
    tokenizer = load_tokenizer(config, pipeline_kwargs)
    transformer = load_transformer(config, pipeline_kwargs)
    streamer = TextIteratorStreamer(
        tokenizer,
        skip_prompt=True,
        skip_special_tokens=True,
    )

    # Build Pipeline
    pipeline = TextPipeline(
        tokenizer=tokenizer,
        transformer=transformer,
        streamer=streamer,
        kwargs = pipeline_kwargs
    )
    return pipeline


#------------------------------------------------
# Generate Text
#------------------------------------------------
def generate(
        inference_args: Dict[str, Any],
        input_tensors: Optional[List[Tuple[Sequence[float],Sequence[int]]]] = None
    ) -> Sequence[Buffer]:
    global _prompt_cache_key, _prompt_cache_value, _stopwatch
    _cancel_event.clear()
    _stopwatch = Utils.Stopwatch()
    _stopwatch.start()
    Utils.notification_push(key="Generate", subkey="Initialize")

    images = Utils.prepare_images(input_tensors)
    image_count = Utils.get_len(images)
    print(f"[Generate] Input Received - Images: {image_count}")

    # Options
    options = DataObjects.GenerateTextOptions(**inference_args)
    #print(f"[Conversation] {options.conversation}")

    # Generation Inputs
    Utils.notification_push(key="Generate", subkey="Tokenizer", elapsedkey="Initialize", elapsed=_stopwatch.reset())
    inputs = generate_text_inputs(pipeline=_pipeline, options= options)

    # Generation Result
    Utils.notification_push(key="Generate", subkey="Transformer", elapsedkey="Tokenizer", elapsed=_stopwatch.reset())
    result = generate_text_result(pipeline=_pipeline, stopwatch=_stopwatch, kwargs=inputs)

    # Cleanup
    Utils.notification_push(key="Generate", subkey="Complete", elapsedkey="Transformer", elapsed = _stopwatch.stop())
    Utils.trim_memory(_isMemoryOffload)

    # Text, Score, Beam, PenaltyScore
    return [ (result, 0, 0.0, 0.0) ]


def generate_text_inputs(pipeline: TextPipeline, options: DataObjects.GenerateTextOptions) -> dict[str, Any]:
    prompt = pipeline.tokenizer.apply_chat_template(
        options.conversation,
        tokenize=False,
        add_generation_prompt=True,
    )

    device = pipeline.transformer.device
    inputs = pipeline.tokenizer(prompt, return_tensors="pt").to(device)
    input_length = inputs["input_ids"].shape[1]
    context_limit = pipeline.transformer.config.max_position_embeddings
    max_new_tokens = options.max_length if options.max_length > 0 else max(1, context_limit - input_length)
    generation_kwargs = dict(
        **inputs,
        streamer=pipeline.streamer,
        max_new_tokens=max_new_tokens,
        do_sample=options.do_sample,
        num_beams=options.num_beams,
        temperature=options.temperature,
        top_k=options.top_k,
        top_p=options.top_p,
        #top_h=options.top_h,
        typical_p=options.typical_p,
        repetition_penalty=options.repetition_penalty,
        length_penalty=options.length_penalty,
        no_repeat_ngram_size=options.no_repeat_ngram_size,
    )
    return generation_kwargs


def generate_text_result(pipeline: TextPipeline, stopwatch: Utils.Stopwatch, kwargs: dict[str, Any]) -> str:
    generate_thread = threading.Thread(
        target=pipeline.transformer.generate,
        kwargs=kwargs,
    )
    generate_thread.start()

    chunks = []
    for chunk in pipeline.streamer:
        chunks.append(chunk)
        Utils.notification_push(key="Generate", subkey="Step", elapsedkey="Step", elapsed=stopwatch.reset(), message=chunk)

    generate_thread.join()
    return "".join(chunks)
