from tensorstack.utils import Stopwatch, notification_push
from tensorstack.data_objects import GenerateTextOptions
import threading
from queue import Queue, Empty
from dataclasses import dataclass
from threading import Event
from typing import Any
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


@dataclass(slots=True)
class TextPipeline:
    tokenizer: PreTrainedTokenizerBase
    processor: ProcessorMixin
    transformer: PreTrainedModel
    streamer: TextIteratorStreamer
    kwargs: dict[str, Any]

    #------------------------------------------------
    # Get the pipeline device_map
    #------------------------------------------------
    def generate_inputs(self, options: GenerateTextOptions, conversation: dict[str, Any], cancel: Event, images: ImageInput) -> dict[str, Any]:
        set_seed(options.seed)
        device = self.transformer.device
        prompt = self._apply_chat_template(conversation)
        inputs = self._get_inputs(prompt, images).to(device)
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
            streamer=self.streamer if options.num_beams == 1 else None,
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
        exceptions = Queue()
        def worker():
            try:
                self.transformer.generate(**kwargs)
            except Exception as e:
                exceptions.put(e)

        generate_thread = threading.Thread(target=worker, daemon=True)
        generate_thread.start()

        chunks = []
        while True:
            try:
                chunk = next(self.streamer)
                chunks.append(chunk)
                notification_push(key="Generate", subkey="Token", elapsedkey="Token", elapsed=stopwatch.reset(), message=chunk)
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

        return [("".join(chunks), 0, 0.0, 0.0)]


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

            results.append((text, beam_idx, score, 0.0))

        return results


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


class CancellationCriteria(StoppingCriteria):
    def __init__(self, event):
        self.event = event

    def __call__(self, input_ids, scores, **kwargs):
        return self.event.is_set()