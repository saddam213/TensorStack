import torch
from typing import Any
import tensorstack.data_objects as DataObjects
from tensorstack.enums import QuantType, QuantBackend, QuantTarget, MemoryMode, VendorType
import bitsandbytes
from optimum.quanto import freeze, quantize, qint8, qint4
from torchao.quantization import Int8WeightOnlyConfig, Int4WeightOnlyConfig
from transformers import (
    QuantoConfig as TransformersQuantoConfig,
    BitsAndBytesConfig as TransformersBitsAndBytesConfig,
    TorchAoConfig as TransformersTorchAoConfig
)
from diffusers import (
    GGUFQuantizationConfig as DiffusersGGUFConfig,
    QuantoConfig as DiffusersQuantoConfig,
    BitsAndBytesConfig as DiffusersBitsAndBytesConfig,
    TorchAoConfig as DiffusersTorchAoConfig
)


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
        print(f"[Quantize] {QuantBackend.QUANTO}, {data_type} -> {quant_type}")
        quantize(model, weights=qint8)
        freeze(model)
    elif quant_type == QuantType.Q4Bit:
        print(f"[Quantize] {QuantBackend.QUANTO}, {data_type} -> {quant_type}")
        quantize(model, weights=qint4)
        freeze(model)


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
        return pretrained_config(target, QuantBackend.NONE, device_vendor, QuantType.Q16Bit, data_type)
    elif target == QuantTarget.TEXT_ENCODER:
        if quant_type == QuantType.Q8Bit:
            return pretrained_config(target, QuantBackend.TorchAO, device_vendor, QuantType.Q8Bit, data_type)
        elif quant_type == QuantType.Q4Bit:
            return pretrained_config(target, QuantBackend.BITSANDBYTES, device_vendor, QuantType.Q4Bit, data_type)

    elif target == QuantTarget.TRANSFORMER:
        if quant_type == QuantType.Q8Bit:
            return pretrained_config(target, QuantBackend.TorchAO, device_vendor, QuantType.Q8Bit, data_type)
        elif quant_type == QuantType.Q4Bit:
            return pretrained_config(target, QuantBackend.BITSANDBYTES, device_vendor, QuantType.Q4Bit, data_type)

    return None


#------------------------------------------------
# Quantization Configuration for from_pretrained
#------------------------------------------------
def pretrained_config(target: QuantTarget, backend: QuantBackend, vendor: VendorType, quant_type: QuantType, compute_type: torch.dtype):
    if quant_type == QuantType.Q16Bit or backend == QuantBackend.NONE:
        print(f"[Quantize] {quant_type} not supported")
        return None

    vendor_quant = get_vendor_quant(backend, quant_type, vendor)
    print(f"[Quantize] {backend}, {compute_type} -> {quant_type} ({vendor_quant})")
    # QUANTO
    if backend == QuantBackend.QUANTO:
        if target == QuantTarget.TEXT_ENCODER:
            if quant_type == QuantType.Q8Bit:
                return TransformersQuantoConfig(weights_dtype=vendor_quant)
            elif quant_type == QuantType.Q4Bit:
                return TransformersQuantoConfig(weights_dtype=vendor_quant)

        elif target == QuantTarget.TRANSFORMER:
            if quant_type == QuantType.Q8Bit:
                return DiffusersQuantoConfig(weights_dtype=vendor_quant)
            elif quant_type == QuantType.Q4Bit:
                return DiffusersQuantoConfig(weights_dtype=vendor_quant)

    #BITSANDBYTES
    elif backend == QuantBackend.BITSANDBYTES:
        if target == QuantTarget.TEXT_ENCODER:
            if quant_type == QuantType.Q8Bit:
                return TransformersBitsAndBytesConfig(load_in_8bit=True)
            elif quant_type == QuantType.Q4Bit:
                return TransformersBitsAndBytesConfig(load_in_4bit=True, bnb_4bit_compute_dtype=compute_type, bnb_4bit_quant_type=vendor_quant)

        elif target == QuantTarget.TRANSFORMER:
            if quant_type == QuantType.Q8Bit:
                return DiffusersBitsAndBytesConfig(load_in_8bit=True)
            elif quant_type == QuantType.Q4Bit:
                return DiffusersBitsAndBytesConfig(load_in_4bit=True, bnb_4bit_compute_dtype=compute_type, bnb_4bit_quant_type=vendor_quant)

    # TorchAO
    elif backend == QuantBackend.TorchAO:
        if target == QuantTarget.TEXT_ENCODER:
            if quant_type == QuantType.Q8Bit:
                return TransformersTorchAoConfig(Int8WeightOnlyConfig(group_size=128, version=2))
            elif quant_type == QuantType.Q4Bit:
                return TransformersTorchAoConfig(Int4WeightOnlyConfig(group_size=128, version=2))

        elif target == QuantTarget.TRANSFORMER:
            if quant_type == QuantType.Q8Bit:
                return DiffusersTorchAoConfig(Int8WeightOnlyConfig(group_size=128, version=2))
            elif quant_type == QuantType.Q4Bit:
                return DiffusersTorchAoConfig(Int4WeightOnlyConfig(group_size=128, version=2))

    return None


#------------------------------------------------
# Get quant datatype
#------------------------------------------------
def get_vendor_quant(backend: QuantBackend, quant: QuantType, vendor: VendorType):
     # QUANTO
    if backend == QuantBackend.QUANTO:
        if quant == QuantType.Q8Bit:
            return "int8" # "float8"
        elif quant == QuantType.Q4Bit:
            return "int4"

    # BITSANDBYTES
    elif backend == QuantBackend.BITSANDBYTES:
        if quant == QuantType.Q8Bit:
            return "int8"
        elif quant == QuantType.Q4Bit:
            return "nf4" if vendor == VendorType.Nvidia else "fp4"

    # TorchAO
    elif backend == QuantBackend.TorchAO:
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
# Quantization Configuration for from_single_file
#------------------------------------------------
def single_file_config(target: QuantTarget, backend: QuantBackend, vendor: VendorType, quant_type: QuantType, compute_type: torch.dtype, is_gguf: bool):
    if is_gguf:
        return DiffusersGGUFConfig(compute_dtype=compute_type)

    return pretrained_config(target, backend, vendor, quant_type, compute_type)