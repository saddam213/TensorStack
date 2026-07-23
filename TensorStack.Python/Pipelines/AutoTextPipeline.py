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
    token_get
)
redirect_output()
create_services()
from tensorstack.llm_utils import (
    TextPipeline,
    get_device_map,
    get_model_config,
    configure_memory
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

    # 1. Load from pretrained folder
    print(f"[Load] Loading Pretrained Tokenizer")
    tokenizer = AutoTokenizer.from_pretrained(
        _model_config["transformer"],
        dtype=config.data_type,
        **pipeline_kwargs
    )
    return tokenizer


#------------------------------------------------
# Load AutoProcessor
#------------------------------------------------
def load_processor(config: PipelineConfig, pipeline_kwargs: Dict[str, str]):
    if _pipeline and _pipeline.processor:
        print(f"[Load] Loading Cached Processor")
        return _pipeline.processor

    try:
        # 1. Load from pretrained folder
        print(f"[Load] Loading Processor")
        return AutoProcessor.from_pretrained(
            _model_config["transformer"],
            dtype=config.data_type,
            **pipeline_kwargs
        )
    except Exception:
        return None


#------------------------------------------------
# Load AutoModelForCausalLM/AutoModelForImageTextToText
#------------------------------------------------
def load_transformer(config: PipelineConfig, pipeline_kwargs: Dict[str, str]):
    if _pipeline and _pipeline.transformer:
        print(f"[Load] Loading Cached Transformer")
        return _pipeline.transformer

    # 1. Load from pretrained folder
    if config.model_type == "Vision":
        print(f"[Load] Loading AutoModelForImageTextToText Transformer")
        transformer = AutoModelForImageTextToText.from_pretrained(
            _model_config["transformer"],
            dtype=config.data_type,
            device_map=_device_map,
            quantization_config=auto_pretrained_config(config, QuantTarget.TEXT_ENCODER),
            **pipeline_kwargs
        )
    else:
        print(f"[Load] Loading AutoModelForCausalLM Transformer")
        transformer = AutoModelForCausalLM.from_pretrained(
            _model_config["transformer"],
            dtype=config.data_type,
            device_map=_device_map,
            quantization_config=auto_pretrained_config(config, QuantTarget.TEXT_ENCODER),
            **pipeline_kwargs
        )
    trim_memory(True)
    return transformer


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
    transformer = load_transformer(config, pipeline_kwargs)

    # Build Pipeline
    pipeline = TextPipeline(
        tokenizer=tokenizer,
        processor=processor,
        transformer=transformer,
        kwargs=pipeline_kwargs
    )
    return pipeline


#------------------------------------------------
# Generate Text
#------------------------------------------------
def generate(
        inference_args: Dict[str, Any],
        input_tensors: Optional[List[Tuple[Sequence[float],Sequence[int]]]] = None
    ) -> Sequence[Buffer]:
    global _stopwatch
    _cancel_event.clear()
    _stopwatch = Stopwatch()
    _stopwatch.start()
    notification_push(key="Generate", subkey="Initialize")

    images = prepare_images(input_tensors)
    print(f"[Generate] Input Received - Images: {get_len(images)}")

    # Options
    options = GenerateTextOptions(**inference_args)

    # Generation Inputs
    notification_push(key="Generate", subkey="Tokenizer", elapsedkey="Initialize", elapsed=_stopwatch.reset())
    inputs = _pipeline.generate_inputs(
        options= options,
        cancel=_cancel_event,
        images=images
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
