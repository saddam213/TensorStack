using Amuse.Common;
using System;

namespace Amuse.Host.Onnx
{
    public static class Extensions
    {
        public static TensorStack.TextGeneration.Pipelines.Whisper.LanguageType GetLanguageType(this Common.GenerateTextOptions options)
        {
            if (Enum.TryParse<TensorStack.TextGeneration.Pipelines.Whisper.LanguageType>(options.Language.GetShortName(), true, out var languageType))
                return languageType;

            return TensorStack.TextGeneration.Pipelines.Whisper.LanguageType.EN;
        }
    }
}
