import torch
from typing import Any
import tensorstack.data_objects as DataObjects
from tensorstack.enums import QuantType, QuantBackend, QuantTarget, MemoryMode, VendorType
import bitsandbytes
from optimum.quanto import freeze, quantize, qint8, qint4
from transformers import (
    QuantoConfig as TransformersQuantoConfig,
    BitsAndBytesConfig as TransformersBitsAndBytesConfig
)
from diffusers import (
    GGUFQuantizationConfig as DiffusersGGUFConfig,
    QuantoConfig as DiffusersQuantoConfig,
    BitsAndBytesConfig as DiffusersBitsAndBytesConfig
)

try:
    from torchao.quantization import (Int8WeightOnlyConfig, Int4WeightOnlyConfig, quantize_)
    from transformers import (TorchAoConfig as TransformersTorchAoConfig)
    from diffusers import (TorchAoConfig as DiffusersTorchAoConfig)
except ImportError:
    Int8WeightOnlyConfig = None
    Int4WeightOnlyConfig = None
    TransformersTorchAoConfig = None
    DiffusersTorchAoConfig = None

#------------------------------------------------
# Quantize a PyTorch model
#------------------------------------------------
def quantize_model(config: DataObjects.PipelineConfig, model: Any, is_gguf: bool):
    if config.memory_mode == MemoryMode.OffloadCPU:
        print(f"[Quantize] OffloadCPU not supported")
        return None
    if is_gguf:
        return

    data_type = config.data_type
    quant_type = config.quant_type
    if quant_type == QuantType.Q16Bit:
        print(f"[Quantize] {quant_type} not supported")
        return
    elif quant_type == QuantType.Q8Bit:
        print(f"[Quantize] {QuantBackend.TORCHAO}, {data_type} -> {quant_type}")
        quantize_(model, Int8WeightOnlyConfig())
    elif quant_type == QuantType.Q4Bit:
        print(f"[Quantize] {QuantBackend.TORCHAO}, {data_type} -> {quant_type}")
        quantize_(model, Int4WeightOnlyConfig())

    # elif quant_type == QuantType.Q8Bit:
    #     print(f"[Quantize] {QuantBackend.QUANTO}, {data_type} -> {quant_type}")
    #     quantize(model, weights=qint8)
    #     freeze(model)
    # elif quant_type == QuantType.Q4Bit:
    #     print(f"[Quantize] {QuantBackend.QUANTO}, {data_type} -> {quant_type}")
    #     quantize(model, weights=qint4)
    #     freeze(model)


#------------------------------------------------
# Auto Quantization Configuration for from_pretrained
#------------------------------------------------
def auto_pretrained_config(config: DataObjects.PipelineConfig, target: QuantTarget):
    if config.memory_mode ==  MemoryMode.OffloadCPU:
        print(f"[Quantize] OffloadCPU not supported")
        return None

    data_type = config.data_type
    quant_type = config.quant_type
    device_vendor = config.device_vendor
    if quant_type == QuantType.Q16Bit:
        return pretrained_config(target, QuantBackend.NONE, QuantType.Q16Bit, data_type)

    # AMD
    if device_vendor == VendorType.AMD:
        if target == QuantTarget.TEXT_ENCODER:
            if quant_type == QuantType.Q8Bit:
                return pretrained_config(target, QuantBackend.BITSANDBYTES, QuantType.Q8Bit, data_type)
            elif quant_type == QuantType.Q4Bit:
                return pretrained_config(target, QuantBackend.BITSANDBYTES, QuantType.Q4Bit, data_type)
        elif target == QuantTarget.TRANSFORMER:
            if quant_type == QuantType.Q8Bit:
                return pretrained_config(target, QuantBackend.BITSANDBYTES, QuantType.Q8Bit, data_type)
            elif quant_type == QuantType.Q4Bit:
                return pretrained_config(target, QuantBackend.BITSANDBYTES, QuantType.Q4Bit, data_type)

    # Nvidia
    elif device_vendor == VendorType.Nvidia:
        if target == QuantTarget.TEXT_ENCODER:
            if quant_type == QuantType.Q8Bit:
                return pretrained_config(target, QuantBackend.TORCHAO, QuantType.Q8Bit, data_type)
            elif quant_type == QuantType.Q4Bit:
                return pretrained_config(target, QuantBackend.BITSANDBYTES, QuantType.Q4Bit, data_type)
        elif target == QuantTarget.TRANSFORMER:
            if quant_type == QuantType.Q8Bit:
                return pretrained_config(target, QuantBackend.TORCHAO, QuantType.Q8Bit, data_type)
            elif quant_type == QuantType.Q4Bit:
                return pretrained_config(target, QuantBackend.BITSANDBYTES, QuantType.Q4Bit, data_type)

    return None


#------------------------------------------------
# Quantization Configuration for from_pretrained
#------------------------------------------------
def pretrained_config(target: QuantTarget, backend: QuantBackend, quant_type: QuantType, compute_type: torch.dtype):
    if quant_type == QuantType.Q16Bit or backend == QuantBackend.NONE:
        print(f"[Quantize] {quant_type} not supported")
        return None

    quant_datatype = get_quant_datatype(backend, quant_type)
    print(f"[Quantize] {backend}, {compute_type} -> {quant_type} ({quant_datatype})")

    # QUANTO
    if backend == QuantBackend.QUANTO:
        if target == QuantTarget.TEXT_ENCODER:
            if quant_type == QuantType.Q8Bit:
                return TransformersQuantoConfig(weights_dtype=quant_datatype)
            elif quant_type == QuantType.Q4Bit:
                return TransformersQuantoConfig(weights_dtype=quant_datatype)
        elif target == QuantTarget.TRANSFORMER:
            if quant_type == QuantType.Q8Bit:
                return DiffusersQuantoConfig(weights_dtype=quant_datatype)
            elif quant_type == QuantType.Q4Bit:
                return DiffusersQuantoConfig(weights_dtype=quant_datatype)

    #BITSANDBYTES
    elif backend == QuantBackend.BITSANDBYTES:
        if target == QuantTarget.TEXT_ENCODER:
            if quant_type == QuantType.Q8Bit:
                return TransformersBitsAndBytesConfig(load_in_8bit=True, llm_int8_enable_fp32_cpu_offload=True, bnb_4bit_compute_dtype=compute_type, bnb_4bit_quant_type=quant_datatype, bnb_4bit_use_double_quant=True)
            elif quant_type == QuantType.Q4Bit:
                return TransformersBitsAndBytesConfig(load_in_4bit=True, llm_int8_enable_fp32_cpu_offload=True, bnb_4bit_compute_dtype=compute_type, bnb_4bit_quant_type=quant_datatype, bnb_4bit_use_double_quant=True)
        elif target == QuantTarget.TRANSFORMER:
            if quant_type == QuantType.Q8Bit:
                return DiffusersBitsAndBytesConfig(load_in_8bit=True, llm_int8_enable_fp32_cpu_offload=True, bnb_4bit_compute_dtype=compute_type, bnb_4bit_quant_type=quant_datatype, bnb_4bit_use_double_quant=True)
            elif quant_type == QuantType.Q4Bit:
                return DiffusersBitsAndBytesConfig(load_in_4bit=True, llm_int8_enable_fp32_cpu_offload=True, bnb_4bit_compute_dtype=compute_type, bnb_4bit_quant_type=quant_datatype, bnb_4bit_use_double_quant=True)

    # TorchAO
    elif backend == QuantBackend.TORCHAO:
        if target == QuantTarget.TEXT_ENCODER:
            if quant_type == QuantType.Q8Bit:
                return TransformersTorchAoConfig(Int8WeightOnlyConfig())
            elif quant_type == QuantType.Q4Bit:
                return TransformersTorchAoConfig(Int4WeightOnlyConfig())
        elif target == QuantTarget.TRANSFORMER:
            if quant_type == QuantType.Q8Bit:
                return DiffusersTorchAoConfig(Int8WeightOnlyConfig())
            elif quant_type == QuantType.Q4Bit:
                return DiffusersTorchAoConfig(Int4WeightOnlyConfig())

    return None


#------------------------------------------------
# Get quant datatype
#------------------------------------------------
def get_quant_datatype(backend: QuantBackend, quant: QuantType):
     # QUANTO
    if backend == QuantBackend.QUANTO:
        if quant == QuantType.Q8Bit:
            return "int8"
        elif quant == QuantType.Q4Bit:
            return "int4"

    # BITSANDBYTES
    elif backend == QuantBackend.BITSANDBYTES:
        if quant == QuantType.Q8Bit:
            return "int8"
        elif quant == QuantType.Q4Bit:
            return "nf4"

    # TorchAO
    elif backend == QuantBackend.TORCHAO:
        if quant == QuantType.Q8Bit:
            return "int8"
        elif quant == QuantType.Q4Bit:
            return "int4"


#------------------------------------------------
# Auto Quantization Configuration for from_single_file
#------------------------------------------------
def auto_single_file_config(config: DataObjects.PipelineConfig, target: QuantTarget, is_gguf: bool):
    if is_gguf:
        return DiffusersGGUFConfig(compute_dtype=config.data_type)

    if config.memory_mode ==  MemoryMode.OffloadCPU:
        print(f"[Quantize] OffloadCPU not supported")
        return None

    return None


#------------------------------------------------
# Auto Quantization Configuration for from_pretrained
#------------------------------------------------
def auto_llm_pretrained_config(config: DataObjects.PipelineConfig):
    if config.memory_mode ==  MemoryMode.OffloadCPU:
        print(f"[Quantize] OffloadCPU not supported")
        return None

    data_type = config.data_type
    quant_type = config.quant_type
    device_vendor = config.device_vendor
    target = QuantTarget.TEXT_ENCODER
    if quant_type == QuantType.Q16Bit:
        return pretrained_config(target, QuantBackend.NONE, QuantType.Q16Bit, data_type)

    # AMD
    if device_vendor == VendorType.AMD:
        if quant_type == QuantType.Q8Bit:
            return pretrained_config(target, QuantBackend.BITSANDBYTES, QuantType.Q8Bit, data_type)
        elif quant_type == QuantType.Q4Bit:
            return pretrained_config(target, QuantBackend.BITSANDBYTES, QuantType.Q4Bit, data_type)

    # Nvidia
    elif device_vendor == VendorType.Nvidia:
        if quant_type == QuantType.Q8Bit:
            return pretrained_config(target, QuantBackend.TORCHAO, QuantType.Q8Bit, data_type)
        elif quant_type == QuantType.Q4Bit:
            return pretrained_config(target, QuantBackend.BITSANDBYTES, QuantType.Q4Bit, data_type)

    return None