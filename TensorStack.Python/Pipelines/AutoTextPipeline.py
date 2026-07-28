from tensorstack.enums import ProcessType, QuantTarget
from tensorstack.quantization import auto_pretrained_config
from tensorstack.data_objects import PipelineConfig, GenerateTextOptions
from tensorstack.utils import (
    Stopwatch,
    redirect_output,
    create_services,
    get_len,
    get_output,
    trim_memory,
    get_execution_device,
    notification_get,
    notification_push,
    prepare_images,
    prepare_audios,
    token_get
)
redirect_output()
create_services()
from tensorstack.llm_utils import (
    TextPipeline,
    get_device_map,
    get_model_config,
    configure_memory,
    apply_chat_template_override
)
import torch
from threading import Event
from collections.abc import Buffer
from typing import Dict, Sequence, List, Tuple, Optional, Any
from transformers import (
    AutoTokenizer,
    AutoProcessor,
    AutoModelForCausalLM,
    AutoModelForImageTextToText
)

# Globals
_config = None
_model_config = None
_pipeline = None
_processType = None
_execution_device = None
_device_map = None
_generator = None
_isMemoryOffload = False
_cancel_event = Event()
_stopwatch = None


#------------------------------------------------
# Load Pipeline
#------------------------------------------------
def load(config_args: Dict[str, Any]) -> bool:
    global _config, _pipeline, _generator, _processType, _execution_device, _isMemoryOffload

    # Config
    _config = PipelineConfig(**config_args)
    _execution_device = get_execution_device(_config)
    _generator = torch.Generator(device=_execution_device)
    _processType = _config.process_type

    # Initialize Pipeline
    _pipeline = initialize(_config)

    # Memory
    _isMemoryOffload = configure_memory(_pipeline, _execution_device, _config)
    trim_memory(_isMemoryOffload)
    return True


#------------------------------------------------
# Reload Pipeline - ProcessType, LoraAdapters and ControlNet are the only options that can be modified
#------------------------------------------------
def reload(config_args: Dict[str, Any]) -> bool:
    global _config, _pipeline, _processType, _isMemoryOffload

    # Config
    _config = PipelineConfig(**config_args)
    _processType = _config.process_type

    # Rebuild Pipeline
    _pipeline = create_pipeline(_config)

    # Memory
    _isMemoryOffload = configure_memory(_pipeline, _execution_device, _config)
    trim_memory(_isMemoryOffload)
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
    global _pipeline
    _pipeline = None
    trim_memory(_isMemoryOffload)
    return True


#------------------------------------------------
# Get the notifications
#------------------------------------------------
def getNotifications() -> list[(str, Buffer)]:
    return notification_get()


#------------------------------------------------
# Get the log entires
#------------------------------------------------
def getLogs() -> list[str]:
    return get_output()


#------------------------------------------------
# Get the token entires
#------------------------------------------------
def getTokens() -> list[str]:
    return token_get()


#------------------------------------------------
# Initialize Pipeline
#------------------------------------------------
def initialize(config: PipelineConfig):
    global _model_config, _device_map

    _device_map = get_device_map(config, _execution_device)
    _model_config = get_model_config(__file__, config)
    return create_pipeline(config)


#------------------------------------------------
# Load AutoTokenizer
#------------------------------------------------
def load_tokenizer(config: PipelineConfig, pipeline_kwargs: Dict[str, str]):
    if _pipeline and _pipeline.tokenizer:
        print(f"[Load] Loading Cached Tokenizer")
        return _pipeline.tokenizer

    tokenizer_path = _model_config["base_model"]
    chat_template = _model_config["chat_template"]

    # 1. Load from pretrained folder
    print(f"[Load] Loading Pretrained Tokenizer")
    tokenizer = AutoTokenizer.from_pretrained(
        tokenizer_path,
        dtype=config.data_type,
        **pipeline_kwargs
    )
    apply_chat_template_override(tokenizer, chat_template)
    return tokenizer


#------------------------------------------------
# Load AutoProcessor
#------------------------------------------------
def load_processor(config: PipelineConfig, pipeline_kwargs: Dict[str, str]):
    if _pipeline and _pipeline.processor:
        print(f"[Load] Loading Cached Processor")
        return _pipeline.processor

    processor_path = _model_config["base_model"]
    chat_template = _model_config["chat_template"]

    try:
        # 1. Load from pretrained folder
        print(f"[Load] Loading Processor")
        processor = AutoProcessor.from_pretrained(
            processor_path,
            dtype=config.data_type,
            **pipeline_kwargs
        )
        apply_chat_template_override(processor, chat_template)
        return processor
    except Exception:
        return None


#------------------------------------------------
# Load AutoModelForCausalLM/AutoModelForImageTextToText
#------------------------------------------------
def load_base_model(config: PipelineConfig, pipeline_kwargs: Dict[str, str]):
    if _pipeline and _pipeline.base_model:
        print(f"[Load] Loading Cached BaseModel")
        return _pipeline.base_model

    # 1. Load from pretrained folder
    if config.model_type in("Vision", "Multi"):
        print(f"[Load] Loading AutoModelForImageTextToText BaseModel")
        base_model = AutoModelForImageTextToText.from_pretrained(
            _model_config["base_model"],
            dtype=config.data_type,
            device_map=_device_map,
            quantization_config=auto_pretrained_config(config, QuantTarget.TEXT_ENCODER),
            **pipeline_kwargs
        )
    else:
        print(f"[Load] Loading AutoModelForCausalLM BaseModel")
        base_model = AutoModelForCausalLM.from_pretrained(
            _model_config["base_model"],
            dtype=config.data_type,
            device_map=_device_map,
            quantization_config=auto_pretrained_config(config, QuantTarget.TEXT_ENCODER),
            **pipeline_kwargs
        )
    trim_memory(True)
    return base_model


#------------------------------------------------
# Create a new pipeline
#------------------------------------------------
def create_pipeline(config: PipelineConfig):
    pipeline_kwargs = {
        "variant": config.variant,
        "use_safetensors":True,
        "low_cpu_mem_usage":True,
        "local_files_only":True,
    }

    # Load Models
    tokenizer = load_tokenizer(config, pipeline_kwargs)
    processor = load_processor(config, pipeline_kwargs)
    base_model = load_base_model(config, pipeline_kwargs)

    # Build Pipeline
    pipeline = TextPipeline(
        tokenizer=tokenizer,
        processor=processor,
        base_model=base_model,
        kwargs=pipeline_kwargs
    )
    return pipeline


#------------------------------------------------
# Generate Text
#------------------------------------------------
def generate(
        inference_args: Dict[str, Any],
        input_images: Optional[List[Tuple[Sequence[float],Sequence[int]]]] = None,
        input_audios: Optional[List[Tuple[Sequence[float],Sequence[int]]]] = None
    ) -> Sequence[Buffer]:
    global _stopwatch
    _cancel_event.clear()
    _stopwatch = Stopwatch()
    _stopwatch.start()
    notification_push(key="Generate", subkey="Initialize")

    # Options
    options = GenerateTextOptions(**inference_args)

    # Input Tensors
    images = prepare_images(input_images)
    audios = prepare_audios(input_audios)
    print(f"[Generate] Input Received - Image: {get_len(images)}")
    print(f"[Generate] Input Received - Audio: {get_len(audios)}, SampleRate: {options.sample_rate}")

    # Generation Inputs
    notification_push(key="Generate", subkey="Tokenizer", elapsedkey="Initialize", elapsed=_stopwatch.reset())
    inputs = _pipeline.generate_inputs(
        options= options,
        cancel=_cancel_event,
        images=images,
        audios=audios
    )

    # Generation Result
    notification_push(key="Generate", subkey="Transformer", elapsedkey="Tokenizer", elapsed=_stopwatch.reset())
    result = _pipeline.generate_result(
        options=options,
        stopwatch=_stopwatch,
        kwargs=inputs
    )

    # Cleanup
    notification_push(key="Generate", subkey="Complete", elapsedkey="Transformer", elapsed = _stopwatch.stop())
    trim_memory(_isMemoryOffload)
    return result
