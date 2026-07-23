from tensorstack.utils import Stopwatch, token_push
from tensorstack.data_objects import PipelineConfig, GenerateTextOptions
from tensorstack.enums import  MemoryMode
import threading
from pathlib import Path
from queue import Queue, Empty
from dataclasses import dataclass
from threading import Event
from typing import Any, Optional
from transformers.image_utils import ImageInput
from transformers import (
    ProcessorMixin,
    TextIteratorStreamer,
    PreTrainedModel,
    PreTrainedTokenizerBase,
    StoppingCriteria,
    StoppingCriteriaList,
    set_seed,
)

class CountingStreamer(TextIteratorStreamer):
    def __init__(self, tokenizer, **kwargs):
        super().__init__(tokenizer, **kwargs)
        self.token_count = 0

    def put(self, value):
        self.token_count += value.numel()
        super().put(value)


@dataclass(slots=True)
class TextPipeline:
    tokenizer: PreTrainedTokenizerBase
    processor: ProcessorMixin
    transformer: PreTrainedModel
    kwargs: dict[str, Any]
    streamer: Optional[CountingStreamer] = None

    #------------------------------------------------
    # Generate the model inputs
    #------------------------------------------------
    def generate_inputs(self, options: GenerateTextOptions, cancel: Event, images: ImageInput) -> dict[str, Any]:
        set_seed(options.seed)
        device = self.transformer.device
        conversation = self._parse_conversation(options, images)
        prompt = self._apply_chat_template(conversation)
        inputs = self._get_inputs(prompt, images).to(device)

        self.streamer = None
        if options.num_beams == 1:
            self.streamer = CountingStreamer(
                self.tokenizer,
                skip_prompt=True,
                skip_special_tokens=True,
                timeout=1
            )

        generation_kwargs = dict(
            **inputs,
            max_new_tokens=options.max_length,
            temperature=options.temperature,
            top_k=options.top_k,
            top_p=options.top_p,
            top_h=options.top_h,
            typical_p=options.typical_p,
            repetition_penalty=options.repetition_penalty,
            length_penalty=options.length_penalty,
            no_repeat_ngram_size=options.no_repeat_ngram_size,
            output_scores=True,
            return_dict_in_generate=True,
            num_beams=options.num_beams,
            num_return_sequences=options.num_beams,
            do_sample=options.do_sample if options.num_beams == 1 else False,
            streamer=self.streamer,
            stopping_criteria=StoppingCriteriaList([
                CancellationCriteria(cancel)
            ])
        )
        return generation_kwargs


    #------------------------------------------------
    # Generate the results
    #------------------------------------------------
    def generate_result(self, options: GenerateTextOptions, stopwatch: Stopwatch, kwargs: dict[str, Any]):
        if options.num_beams > 1:
            return self._generate_beam_result(kwargs)

        return self._generate_text_result(stopwatch, kwargs)


    #------------------------------------------------
    # Generate the greedy results (supports streaming)
    #------------------------------------------------
    def _generate_text_result(self, stopwatch: Stopwatch, kwargs: dict[str, Any]):
        results = Queue()
        exceptions = Queue()
        def worker():
            try:
                result = self.transformer.generate(**kwargs)
                results.put(result)
            except Exception as e:
                exceptions.put(e)

        generate_thread = threading.Thread(target=worker, daemon=True)
        generate_thread.start()

        chunks = []
        while True:
            try:
                chunk = next(self.streamer)
                chunks.append(chunk)
                token_push(token=chunk,token_count=self.streamer.token_count, elapsed=stopwatch.reset())
                #print(f"[DEBUG] [TokenPush] Chunk: {chunk}")
            except StopIteration:
                break
            except Empty:
                if not exceptions.empty():
                    raise exceptions.get()
                if not generate_thread.is_alive():
                    break

        generate_thread.join()
        if not exceptions.empty():
            raise exceptions.get()

        result = results.get()
        total_tokens = result.sequences.shape[-1]
        return [("".join(chunks), 0, 0.0, total_tokens)]


    #------------------------------------------------
    # Generate the beam results
    #------------------------------------------------
    def _generate_beam_result(self, kwargs: dict[str, Any]):
        results = []
        input_length = kwargs["input_ids"].shape[-1]
        output = self.transformer.generate(**kwargs)
        for beam_idx, sequence in enumerate(output.sequences):
            score = 0.0
            text = self.tokenizer.decode(sequence[input_length:], skip_special_tokens=True)
            if output.sequences_scores is not None:
                score = float(output.sequences_scores[beam_idx])

            results.append((text, beam_idx, score, sequence.shape[-1]))

        return results


    #------------------------------------------------
    # Parse conversation messages
    #------------------------------------------------
    def _parse_conversation(self, options: GenerateTextOptions, images: Any | list[Any]) -> list[dict[str, Any]]:
        messages = []
        if options.conversation is None:
            return messages

        if not isinstance(images, list):
            images = [images]

        #print(f"[DEBUG] Conversation Before: {options.conversation}")
        for message in options.conversation:
            image_indices = message.get("image_index", [])
            role = message["role"]
            text = message["content"]

            if not image_indices:
                messages.append({ "role": role, "content": text })
                continue

            content = []
            for idx in image_indices:
                content.append({ "type": "image", "image": images[idx] })

            content.append({ "type": "text", "text": text })
            messages.append({ "role": role, "content": content })

        #print(f"[DEBUG] Conversation After: {messages}")
        return messages


    #------------------------------------------------
    # Apply the chat template to the conversation
    #------------------------------------------------
    def _apply_chat_template(self, conversation: dict[str, Any]) -> str:
        if self.processor:
            return self.processor.apply_chat_template(
                conversation,
                tokenize=False,
                add_generation_prompt=True,
            )
        elif self.tokenizer is not None:
            return self.tokenizer.apply_chat_template(
                conversation,
                tokenize=False,
                add_generation_prompt=True,
            )
        return None


    #------------------------------------------------
    # Get the pipeline device_map
    #------------------------------------------------
    def _get_inputs(self, prompt: str, images):
        if self.processor:
            return self.processor(text=prompt, images=images, return_tensors="pt")

        return self.tokenizer(prompt, return_tensors="pt")


#------------------------------------------------
# Event based StoppingCriteria
#------------------------------------------------
class CancellationCriteria(StoppingCriteria):
    def __init__(self, event):
        self.event = event

    def __call__(self, input_ids, scores, **kwargs):
        return self.event.is_set()


#------------------------------------------------
# Create model configuration
#------------------------------------------------
def get_model_config(file_path: str, config: PipelineConfig):
    template_path= Path(file_path).resolve().parent / "Templates" / config.template

    # Configs
    transformer_config = template_path / "transformer" / "config.json"

    # Paths
    transformer_path = Path(config.checkpoint_config.transformer) if config.checkpoint_config.transformer else None
    single_file = transformer_path if transformer_path and transformer_path.is_file() else None

    _model_config = {
        "template": template_path,
        "single_file": single_file,
        "transformer": transformer_path,
        "transformer_config": transformer_config,
    }

    info_1 = f"\n\tTemplate: {config.template} \n\tModelType: {config.model_type} \n\tModelPath: {config.model_path} \n\tTemplatePath: {template_path}\n\tTransformer: {transformer_path}"
    print(f"[Load] Initialize Model... \n[ {info_1} \n]")
    return _model_config


#------------------------------------------------
# Get the model device_map
#------------------------------------------------
def get_device_map(config: PipelineConfig, execution_device: str):
    if config.memory_mode == MemoryMode.OffloadGPU:
        return execution_device
    return "auto"


#------------------------------------------------
# Configure pipeline RAM/VRAM offloading
#------------------------------------------------
def configure_memory(pipeline: TextPipeline, execution_device: str, config: PipelineConfig) -> bool:
    if config.memory_mode == MemoryMode.OffloadGPU:
        pipeline.transformer.to(execution_device)
    return config.memory_mode in (MemoryMode.OffloadCPU, MemoryMode.OffloadModel)