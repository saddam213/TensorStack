using Amuse.Common.Message;

namespace Amuse.Common
{
    public sealed record PipelineRunOptions
    {
        public GenerateImageOptions ImageOptions { get; set; }
        public GenerateVideoOptions VideoOptions { get; set; }
        public GenerateAudioOptions AudioOptions { get; set; }
        public GenerateTextOptions TextOptions { get; set; }


        public void PackTensors(PipelineRequest request)
        {
            ImageOptions?.PackTensors(request);
            VideoOptions?.PackTensors(request);
            AudioOptions?.PackTensors(request);
            TextOptions?.PackTensors(request);
        }


        public void UnpackTensors(PipelineRequest request)
        {
            ImageOptions?.UnpackTensors(request);
            VideoOptions?.UnpackTensors(request);
            AudioOptions?.UnpackTensors(request);
            TextOptions?.UnpackTensors(request);
        }
    }
}
