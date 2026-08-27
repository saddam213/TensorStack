using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TensorStack.Common.Common;
using TensorStack.Common.Tensor;
using TensorStack.Media.Video;

namespace TensorStack.Media
{
    public static class MediaManager
    {
        internal static readonly string[] VideoEncoders;
        internal static string FFMpegPath = "ffmpeg.exe";
        internal static string FFProbePath = "ffprobe.exe";
        internal static string DirectoryTemp = "Temp";

        static MediaManager()
        {
            VideoEncoders =
            [
                "h264_nvenc",      // NVIDIA
                "h264_amf",        // AMD
                "h264_qsv",        // Intel
                "libopenh264",     // CPU fallback
            ];
        }

        /// <summary>
        /// Initializes the MediaManager.
        /// </summary>
        /// <param name="ffmpegPath">The ffmpeg path.</param>
        /// <param name="ffprobePath">The ffprobe path.</param>
        /// <param name="directoryTemp">The temporary directory.</param>
        public static void Initialize(string ffmpegPath = default, string ffprobePath = default, string directoryTemp = default)
        {
            if (!string.IsNullOrEmpty(ffmpegPath))
                FFMpegPath = ffmpegPath;
            if (!string.IsNullOrEmpty(ffprobePath))
                FFProbePath = ffprobePath;
            if (!string.IsNullOrEmpty(directoryTemp))
                DirectoryTemp = directoryTemp;
        }


        /// <summary>
        /// Saves the VideoSequence to file.
        /// </summary>
        /// <param name="videoSequence">The video sequence.</param>
        /// <param name="filename">The filename.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public static async Task SaveAsync(this VideoSequence videoSequence, string filename, CancellationToken cancellationToken = default)
        {
            foreach (var encoder in VideoEncoders)
            {
                try
                {
                    if (await WriteFramesAsync(videoSequence.Frames, videoSequence.FrameRate, filename, encoder, cancellationToken))
                    {
                        if (videoSequence.Audio != null)
                            await WriteAudioAsync(videoSequence.Audio, filename, cancellationToken);

                        return;
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception) { /* Codec not avaliable */ }
            }
        }


        /// <summary>
        /// Writes the ImageTensor frames to file.
        /// </summary>
        /// <param name="frames">The frames.</param>
        /// <param name="frameRate">The frame rate.</param>
        /// <param name="filename">The filename.</param>
        /// <param name="encoder">The encoder.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        internal static async Task<bool> WriteFramesAsync(ImageTensor[] frames, float frameRate, string filename, string encoder, CancellationToken cancellationToken = default)
        {
            var first = frames[0];
            var width = first.Width;
            var height = first.Height;
            using (var frameWriter = CreateFrameWriter(filename, width, height, frameRate, encoder))
            {
                frameWriter.Start();
                await using (var inputStream = frameWriter.StandardInput.BaseStream)
                {
                    byte[] buffer = new byte[checked(width * height * 4)];
                    int pixels = checked(width * height);
                    for (int f = 0; f < frames.Length; f++)
                    {
                        ReadOnlySpan<float> source = frames[f].Memory.Span;
                        int rOffset = 0;
                        int gOffset = pixels;
                        int bOffset = pixels * 2;
                        int aOffset = pixels * 3;
                        for (int i = 0; i < pixels; i++)
                        {
                            buffer[i * 4 + 0] = ToByte(source[rOffset + i]);
                            buffer[i * 4 + 1] = ToByte(source[gOffset + i]);
                            buffer[i * 4 + 2] = ToByte(source[bOffset + i]);
                            buffer[i * 4 + 3] = ToByte(source[aOffset + i]);
                        }
                        await inputStream.WriteAsync(buffer, cancellationToken);
                    }

                    await inputStream.FlushAsync(cancellationToken);
                    inputStream.Close();
                }

                await frameWriter.WaitForExitAsync(cancellationToken);
                return frameWriter.ExitCode == 0;
            }
        }


        /// <summary>
        /// Writes the AudioTensor to the specified video.
        /// </summary>
        /// <param name="audioTensor">The audio tensor.</param>
        /// <param name="videoFile">The video file.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        private static async Task<bool> WriteAudioAsync(AudioTensor audioTensor, string videoFile, CancellationToken cancellationToken = default)
        {
            var tempFile = FileHelper.RandomFileName("mp4");
            try
            {
                using (var audioWriter = CreateAudioMuxer(videoFile, tempFile, audioTensor))
                {
                    audioWriter.Start();
                    var audioBuffer = CreateAudioBufferInterleaved(audioTensor, audioTensor.Channels, audioTensor.Samples);
                    await audioWriter.StandardInput.BaseStream.WriteAsync(audioBuffer, cancellationToken);
                    await audioWriter.StandardInput.BaseStream.FlushAsync(cancellationToken);
                    audioWriter.StandardInput.Close();
                    await audioWriter.WaitForExitAsync(cancellationToken);
                    if (audioWriter.ExitCode != 0)
                        return false;

                    File.Move(tempFile, videoFile, true);
                    return true;
                }
            }
            finally
            {
                FileHelper.DeleteFile(tempFile);
            }
        }


        /// <summary>
        /// Creates the frame writer.
        /// </summary>
        /// <param name="outputFile">The output file.</param>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        /// <param name="framerate">The framerate.</param>
        /// <param name="encoder">The encoder.</param>
        private static Process CreateFrameWriter(string outputFile, int width, int height, float framerate, string encoder)
        {
            var arguments = $"-hide_banner -f rawvideo -pix_fmt rgba -video_size {width}x{height} -framerate {framerate} -i - -c:v {encoder} -pix_fmt yuv420p -movflags +faststart -y \"{outputFile}\"";
            var process = CreateProcess(FFMpegPath, arguments);
            process.StartInfo.RedirectStandardInput = true;
            return process;
        }


        /// <summary>
        /// Creates the audio muxer.
        /// </summary>
        /// <param name="videoFile">The video file.</param>
        /// <param name="outputFile">The output file.</param>
        /// <param name="audioTensor">The audio tensor.</param>
        private static Process CreateAudioMuxer(string videoFile, string outputFile, AudioTensor audioTensor)
        {
            var arguments = $"-hide_banner -i \"{videoFile}\" -f f32le -ar {audioTensor.SampleRate} -ac {audioTensor.Channels} -i pipe:0 -map 0:v:0 -map 1:a:0 -c:v copy -c:a aac -b:a 192k -shortest -movflags +faststart -y \"{outputFile}\"";
            var process = CreateProcess(FFMpegPath, arguments);
            process.StartInfo.RedirectStandardInput = true;
            return process;
        }


        /// <summary>
        /// Creates the audio interleaved buffer, this uses the flat buffer not the tensor dimensions.
        /// </summary>
        /// <param name="audioTensor">The audio tensor.</param>
        /// <param name="channels">The channels.</param>
        /// <param name="samples">The samples.</param>
        private static byte[] CreateAudioBufferInterleaved(AudioTensor audioTensor, int channels, int samples)
        {
            int totalSamples = checked(channels * samples);
            byte[] buffer = new byte[checked(totalSamples * sizeof(float))];

            ReadOnlySpan<float> source = audioTensor.Memory.Span;
            Span<byte> destination = buffer.AsSpan();

            for (int i = 0; i < totalSamples; i++)
            {
                float sample = Math.Clamp(source[i], -1f, 1f);
                BitConverter.TryWriteBytes(destination.Slice(i * sizeof(float), sizeof(float)), sample);
            }
            return buffer;
        }


        /// <summary>
        /// Convert tensor value to byte.
        /// </summary>
        /// <param name="value">The value.</param>
        private static byte ToByte(float value)
        {
            value = Math.Clamp(value, -1f, 1f);
            return (byte)Math.Round((value + 1f) * 127.5f);
        }


        /// <summary>
        /// Executes the FFMPEG executable.
        /// </summary>
        /// <param name="arguments">The arguments.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        internal static async Task ExecuteFFMPEGAsync(string arguments, CancellationToken cancellationToken = default)
        {
            using (var process = CreateProcess(FFMpegPath, arguments))
            {
                process.Start();
                await process.WaitForExitAsync(cancellationToken);
            }
        }


        /// <summary>
        /// Executes the FFProbe executable.
        /// </summary>
        /// <param name="arguments">The arguments.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        internal static async Task ExecuteFFProbeAsync(string arguments, CancellationToken cancellationToken = default)
        {
            using (var process = CreateProcess(FFProbePath, arguments))
            {
                process.Start();
                await process.WaitForExitAsync(cancellationToken);
            }
        }


        /// <summary>
        /// Creates the process.
        /// </summary>
        /// <param name="executable">The executable.</param>
        /// <param name="arguments">The arguments.</param>
        internal static Process CreateProcess(string executable, string arguments)
        {
            var process = new Process();
            process.StartInfo.FileName = executable;
            process.StartInfo.Arguments = arguments;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            return process;
        }

    }
}
