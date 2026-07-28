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
from transformers.audio_utils import AudioInput
from transformers.video_utils import VideoInput
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
    base_model: PreTrainedModel
    kwargs: dict[str, Any]
    streamer: Optional[CountingStreamer] = None
    special_replacements: tuple[tuple[str, str], ...] = ()
    def __post_init__(self):
        self._build_token_replacements()

    #------------------------------------------------
    # Generate the model inputs
    #------------------------------------------------
    def generate_inputs(self, options: GenerateTextOptions, cancel: Event, images: ImageInput = None, audios: AudioInput = None, videos: VideoInput = None) -> dict[str, Any]:
        set_seed(options.seed)
        device = self.base_model.device
        conversation = self._parse_conversation(options)
        prompt = self._apply_chat_template(options, conversation)
        inputs = self._get_inputs(prompt, images, audios, videos).to(device)

        self.streamer = None
        if options.num_beams == 1:
            self.streamer = CountingStreamer(
                self.tokenizer,
                skip_prompt=True,
                timeout=1,
                skip_special_tokens=False
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
                result = self.base_model.generate(**kwargs)
                results.put(result)
            except Exception as e:
                exceptions.put(e)

        generate_thread = threading.Thread(target=worker, daemon=True)
        generate_thread.start()

        chunks = []
        while True:
            try:
                chunk = next(self.streamer)
                chunk = self._replace_tokens(chunk)
                chunks.append(chunk)
                elapsed = stopwatch.reset()
                token_count=self.streamer.token_count
                token_push(token=chunk,token_count=token_count, elapsed=elapsed)
                #print(f"[DEBUG] [TokenPush] Tokens: {token_count}, TPS: {1000 / elapsed}, Chunk: {chunk}")
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
        output = self.base_model.generate(**kwargs)
        for beam_idx, sequence in enumerate(output.sequences):
            score = 0.0
            text = self.tokenizer.decode(sequence[input_length:], skip_special_tokens=False)
            text = self._replace_tokens(text)
            if output.sequences_scores is not None:
                score = float(output.sequences_scores[beam_idx])

            results.append((text, beam_idx, score, sequence.shape[-1]))

        return results


    #------------------------------------------------
    # Parse conversation messages
    #------------------------------------------------
    def _parse_conversation(self, options: GenerateTextOptions) -> list[dict[str, Any]]:
        messages = []
        if options.conversation is None:
            return messages

        #print(f"[DEBUG] Conversation Before: {options.conversation}")
        for message in options.conversation:
            image_indices = message.get("image_index", [])
            audio_indices = message.get("audio_index", [])
            role = message["role"]
            text = message["content"]
            if not image_indices and not audio_indices:
                messages.append({ "role": role, "content": text })
                continue

            # Image Placeholders
            content = []
            for idx in image_indices:
                content.append({ "type": "image"})

            # Text Content
            content.append({ "type": "text", "text": text })

             # Audio Placeholders
            for idx in audio_indices:
                content.append({ "type": "audio"})

            messages.append({ "role": role, "content": content })

        #print(f"[DEBUG] Conversation After: {messages}")
        return messages


    #------------------------------------------------
    # Apply the chat template to the conversation
    #------------------------------------------------
    def _apply_chat_template(self, options: GenerateTextOptions, conversation: dict[str, Any]) -> str:
        if self.processor:
            return self.processor.apply_chat_template(
                conversation,
                tokenize=False,
                add_generation_prompt=True,
                enable_thinking=options.enable_thinking
            )
        elif self.tokenizer is not None:
            return self.tokenizer.apply_chat_template(
                conversation,
                tokenize=False,
                add_generation_prompt=True,
                enable_thinking=options.enable_thinking
            )
        return None


    #------------------------------------------------
    # Get the pipeline device_map
    #------------------------------------------------
    def _get_inputs(self, prompt: str, images: ImageInput, audios: AudioInput, videos: VideoInput):
        if self.processor:
            return self.processor(
                text=prompt,
                images=images,
                audio=audios,
                videos=videos,
                return_tensors="pt"
            )
        return self.tokenizer(prompt, return_tensors="pt")


    #------------------------------------------------
    # Build token replacement map
    #------------------------------------------------
    def _build_token_replacements(self):
        replacements = []
        thinking_start = { "<|channel>" }
        thinking_end = { "<channel|>" }
        for token in self.tokenizer.all_special_tokens:
            if token in thinking_start:
                replacements.append((token, "<think>\n"))
            elif token in thinking_end:
                replacements.append((token, "\n</think>\n"))
            else:
                replacements.append((token, ""))
        self.special_replacements = tuple(replacements)


    #------------------------------------------------
    # Replace tokens/segments
    #------------------------------------------------
    def _replace_tokens(self, text: str) -> str:
        if "<" not in text:
            return text
        for src, dst in self.special_replacements:
            text = text.replace(src, dst)
        return text

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
    base_model_config = template_path / "config.json"
    chat_template_file, chat_template = load_chat_template(template_path / "chat_template.jinja")

    # Paths
    base_model_path = Path(config.checkpoint_config.text_encoder) if config.checkpoint_config.text_encoder else None
    single_file = base_model_path if base_model_path and base_model_path.is_file() else None

    _model_config = {
        "template": template_path,
        "single_file": single_file,
        "base_model": base_model_path,
        "base_model_config": base_model_config,
        "chat_template": chat_template
    }

    info_1 = f"\n\tModelType: {config.model_type}\n\tTemplate: {config.template}\n\tTemplatePath: {template_path}\n\tChatTemplate: {chat_template_file}\n\tModelPath: {config.model_path} \n\tBaseModel: {base_model_path}"
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
        pipeline.base_model.to(execution_device)
    return config.memory_mode in (MemoryMode.OffloadCPU, MemoryMode.OffloadModel)


#------------------------------------------------
# Load chat template from file
#------------------------------------------------
def load_chat_template(template_path: Path):
    if not template_path.exists():
        return None, None

    chat_template = None
    with open(template_path, "r", encoding="utf-8") as f:
        chat_template = f.read()

    return template_path, chat_template


#------------------------------------------------
# Override default template with out own
#------------------------------------------------
def apply_chat_template_override(tokenizer, chat_template):
    if not chat_template:
        return
    # Tokenizer template
    if hasattr(tokenizer, "chat_template"):
        tokenizer.chat_template = chat_template
    # Processor tokenizer
    if hasattr(tokenizer, "tokenizer") and tokenizer.tokenizer is not None:
        tokenizer.tokenizer.chat_template = chat_template