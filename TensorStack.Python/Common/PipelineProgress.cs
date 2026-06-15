using System;
using System.Globalization;
using System.Text.Json.Serialization;
using TensorStack.Common.Pipeline;
using TensorStack.Common.Tensor;

namespace TensorStack.Python.Common
{
    public record PipelineProgress : IRunProgress
    {
        public string Key { get; init; }
        public string Subkey { get; init; }
        public string ElapsedKey { get; init; }
        public DateTime Timestamp { get; init; }
        public float Elapsed { get; init; }
        public int Value { get; init; }
        public int Maximum { get; init; }
        public int BatchValue { get; init; }
        public int BatchMaximum { get; init; }
        public string Message { get; init; }

        [JsonIgnore]
        public Tensor<float> Tensor { get; init; }

        public float IterationsPerSecond => Elapsed > 0 ? 1000f / Elapsed : 0;
        public float SecondsPerIteration => Elapsed > 0 ? Elapsed / 1000f : 0;

        public readonly static IProgress<PipelineProgress> ConsoleCallback = new Progress<PipelineProgress>(Console.WriteLine);


        public static PipelineProgress Create(string inputData, Tensor<float> tensor)
        {
            if (string.IsNullOrWhiteSpace(inputData))
                return null;

            // {Key}|{Subkey}|{elapsedkey}|{Timestamp}|{Elapsed}|{Value}|{Maximum}|{BatchValue}|{BatchMaximum}|{Message}
            var parameters = inputData.Split('|', 10, StringSplitOptions.TrimEntries);
            if (parameters.Length < 10)
                return null;

            return new PipelineProgress
            {
                Key = parameters[0],
                Subkey = parameters[1],
                ElapsedKey = parameters[2],
                Timestamp = DateTime.Parse(parameters[3], CultureInfo.InvariantCulture),
                Elapsed = float.Parse(parameters[4], CultureInfo.InvariantCulture),
                Value = int.Parse(parameters[5], CultureInfo.InvariantCulture),
                Maximum = int.Parse(parameters[6], CultureInfo.InvariantCulture),
                BatchValue = int.Parse(parameters[7], CultureInfo.InvariantCulture),
                BatchMaximum = int.Parse(parameters[8], CultureInfo.InvariantCulture),
                Message = parameters[9],
                Tensor = tensor
            };
        }

    }
}
