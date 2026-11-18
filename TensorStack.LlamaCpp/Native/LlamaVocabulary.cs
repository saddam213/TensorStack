using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TensorStack.LlamaCpp.Templates;

namespace TensorStack.LlamaCpp.Native
{
    internal sealed unsafe class LlamaVocabulary
    {
        private const string ThinkStart = "<think>\n";
        private const string ThinkEnd = "\n</think>\n";
        private const string ToolCallStart = "<tool_call>\n";
        private const string ToolCallEnd = "\n</tool_call>\n";
        private static readonly char[] _sanitizeTrim = ['[', ']', '<', '>', '|'];
        private static readonly (string Token, string Replacement)[] _replacements =
        [
            ("<|tool_call>", ToolCallStart), ("<tool_call>", ToolCallStart),
            ("<tool_call|>", ToolCallEnd), ("</tool_call>", ToolCallEnd),
            ("<think>", ThinkStart), ("<|channel>", ThinkStart),
            ("</think>", ThinkEnd), ("<channel|>", ThinkEnd),
            ("<|\"|>", "\"")
        ];

        private readonly nint _vocab;
        private readonly int _tokenCount;
        private readonly HashSet<int> _controlTokens = [];
        private readonly HashSet<int> _stopTokens = [];
        private readonly Dictionary<int, string> _tokenReplacements = [];
        private readonly HashSet<string> _sanitizeTokens = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<char> _sanitizeStarts = [];
        private readonly int[] _sanitizeLengths;
        private readonly char[] _sanitizeStartChars;

        /// <summary>
        /// Initializes a new instance of the <see cref="LlamaVocabulary"/> class.
        /// </summary>
        /// <param name="vocab">The native vocabulary.</param>
        /// <param name="formatter">The prompt formatter.</param>
        /// <param name="mediaMarker">The marker that is replaced by an input image or audio.</param>
        public LlamaVocabulary(nint vocab, PromptFormatter formatter, string mediaMarker)
        {
            _vocab = vocab;
            _tokenCount = NativeApi.llama_vocab_n_tokens(vocab);

            // Control tokens, they are removed from the output and from message content
            for (int token = 0; token < _tokenCount; token++)
            {
                if (!NativeApi.llama_vocab_get_attr(vocab, token).HasFlag(NativeApi.TokenAttribute.Control))
                    continue;

                _controlTokens.Add(token);
                AddSanitizeToken(NativeApi.PtrToString(NativeApi.llama_vocab_get_text(vocab, token)));
            }

            foreach (var reservedToken in formatter.ReservedTokens)
            {
                AddSanitizeToken(reservedToken);
            }
            AddSanitizeToken(mediaMarker);
            _sanitizeLengths = [.. _sanitizeTokens.Select(x => x.Length).Distinct().OrderByDescending(x => x)];
            _sanitizeStartChars = [.. _sanitizeStarts];

            // Stop tokens
            foreach (var stopToken in formatter.StopTokens)
            {
                if (TryGetToken(stopToken, out var token))
                    _stopTokens.Add(token);
            }

            // Output replacements, thinking and tool call tokens are normalized to the same tags for every model
            foreach (var replacement in _replacements)
            {
                if (TryGetToken(replacement.Token, out var token))
                    _tokenReplacements[token] = replacement.Replacement;
            }
        }

        /// <summary>
        /// Gets the number of tokens in the vocabulary.
        /// </summary>
        public int TokenCount => _tokenCount;


        /// <summary>
        /// Tokenizes the text.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="isAddSpecial">if set to <c>true</c> the begin token is added when the model requires it.</param>
        /// <param name="isParseSpecial">if set to <c>true</c> special tokens in the text are parsed as tokens.</param>
        public int[] Tokenize(string text, bool isAddSpecial, bool isParseSpecial)
        {
            var bytes = Encoding.UTF8.GetBytes(text ?? string.Empty);
            var tokens = new int[bytes.Length + 8];
            fixed (byte* bytesPtr = bytes)
            fixed (int* tokensPtr = tokens)
            {
                var count = NativeApi.llama_tokenize(_vocab, bytesPtr, bytes.Length, tokensPtr, tokens.Length, isAddSpecial, isParseSpecial);
                if (count >= 0)
                    return tokens.AsSpan(0, count).ToArray();

                // A negative count is the size that is required
                tokens = new int[-count];
                fixed (int* resizedPtr = tokens)
                {
                    count = NativeApi.llama_tokenize(_vocab, bytesPtr, bytes.Length, resizedPtr, tokens.Length, isAddSpecial, isParseSpecial);
                    if (count < 0)
                        throw new Exception("llama.cpp failed to tokenize the prompt.");

                    return tokens.AsSpan(0, count).ToArray();
                }
            }
        }


        /// <summary>
        /// Determines whether the token ends the generation.
        /// </summary>
        /// <param name="token">The token.</param>
        public bool IsStopToken(int token)
        {
            return _stopTokens.Contains(token) || NativeApi.llama_vocab_is_eog(_vocab, token);
        }


        /// <summary>
        /// Creates a decoder that converts generated tokens to normalized text.
        /// </summary>
        public TokenDecoder CreateDecoder()
        {
            return new TokenDecoder(this);
        }


        /// <summary>
        /// Removes special tokens from message content so content can not alter the prompt structure, the token delimiters are stripped (e.g. &lt;|im_end|&gt; becomes im_end).
        /// </summary>
        /// <param name="content">The content.</param>
        public string Sanitize(string content)
        {
            if (string.IsNullOrEmpty(content) || content.IndexOfAny(_sanitizeStartChars) < 0)
                return content;

            var builder = new StringBuilder(content.Length);
            var index = 0;
            while (index < content.Length)
            {
                var match = _sanitizeStarts.Contains(content[index]) ? FindSanitizeToken(content, index) : null;
                if (match == null)
                {
                    builder.Append(content[index++]);
                    continue;
                }

                builder.Append(match.Trim(_sanitizeTrim));
                index += match.Length;
            }
            return builder.ToString();
        }


        /// <summary>
        /// Finds the longest special token that starts at the index.
        /// </summary>
        /// <param name="content">The content.</param>
        /// <param name="index">The index.</param>
        private string FindSanitizeToken(string content, int index)
        {
            foreach (var length in _sanitizeLengths)
            {
                if (index + length > content.Length)
                    continue;

                var candidate = content.Substring(index, length);
                if (_sanitizeTokens.Contains(candidate))
                    return candidate;
            }
            return null;
        }


        /// <summary>
        /// Adds a token that is removed from message content.
        /// </summary>
        /// <param name="token">The token.</param>
        private void AddSanitizeToken(string token)
        {
            // Only delimited tokens can be stripped, plain word tokens are left as text
            if (string.IsNullOrEmpty(token) || token.Trim(_sanitizeTrim).Length == token.Length)
                return;

            _sanitizeTokens.Add(token);
            _sanitizeStarts.Add(token[0]);
        }


        /// <summary>
        /// Gets the token of a text that is a single token in the vocabulary.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="token">The token.</param>
        private bool TryGetToken(string text, out int token)
        {
            var tokens = Tokenize(text, false, true);
            token = tokens.Length == 1 ? tokens[0] : -1;
            return tokens.Length == 1;
        }


        /// <summary>
        /// Gets the UTF8 bytes of a token.
        /// </summary>
        /// <param name="token">The token.</param>
        /// <param name="buffer">The buffer.</param>
        /// <returns>The number of bytes written.</returns>
        private int GetTokenBytes(int token, ref byte[] buffer)
        {
            fixed (byte* bufferPtr = buffer)
            {
                var count = NativeApi.llama_token_to_piece(_vocab, token, bufferPtr, buffer.Length, 0, true);
                if (count >= 0)
                    return count;
            }

            // A negative count is the size that is required
            buffer = new byte[-NativeApi.llama_token_to_piece(_vocab, token, null, 0, 0, true)];
            fixed (byte* bufferPtr = buffer)
            {
                return Math.Max(0, NativeApi.llama_token_to_piece(_vocab, token, bufferPtr, buffer.Length, 0, true));
            }
        }


        /// <summary>
        /// Converts generated tokens to text, a character can span several tokens so incomplete characters are held until they are complete.
        /// </summary>
        internal sealed class TokenDecoder
        {
            private readonly LlamaVocabulary _vocabulary;
            private readonly Decoder _decoder = Encoding.UTF8.GetDecoder();
            private byte[] _byteBuffer = new byte[256];
            private char[] _charBuffer = new char[256];

            /// <summary>
            /// Initializes a new instance of the <see cref="TokenDecoder"/> class.
            /// </summary>
            /// <param name="vocabulary">The vocabulary.</param>
            public TokenDecoder(LlamaVocabulary vocabulary)
            {
                _vocabulary = vocabulary;
            }


            /// <summary>
            /// Decodes the token, thinking and tool call tokens are normalized and all other control tokens are removed.
            /// </summary>
            /// <param name="token">The token.</param>
            /// <returns>The text, empty when the token has no visible text yet.</returns>
            public string Decode(int token)
            {
                if (_vocabulary._tokenReplacements.TryGetValue(token, out var replacement))
                    return replacement;
                if (_vocabulary._controlTokens.Contains(token))
                    return string.Empty;

                var byteCount = _vocabulary.GetTokenBytes(token, ref _byteBuffer);
                if (byteCount == 0)
                    return string.Empty;

                var charCount = _decoder.GetCharCount(_byteBuffer, 0, byteCount, false);
                if (charCount > _charBuffer.Length)
                    _charBuffer = new char[charCount];

                charCount = _decoder.GetChars(_byteBuffer, 0, byteCount, _charBuffer, 0, false);
                return charCount == 0 ? string.Empty : new string(_charBuffer, 0, charCount);
            }
        }
    }
}
