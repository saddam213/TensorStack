using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using TensorStack.Common;
using TensorStack.Common.Tensor;
using TensorStack.StableDiffusionCpp.Common;

namespace TensorStack.StableDiffusionCpp.Native
{
    /// <summary>
    /// Owns a native <c>sd_opt_t</c> option object.
    /// Values left unset keep the library default, the setters here skip values that mean "unset".
    /// The library copies or retains everything it is given, so inputs can be freed once a setter returns.
    /// </summary>
    internal sealed unsafe class NativeOptions : IDisposable
    {
        private readonly string _name;
        private NativeApi.sd_opt_t* _handle;

        /// <summary>
        /// Initializes a new instance of the <see cref="NativeOptions"/> class.
        /// </summary>
        /// <param name="handle">The handle.</param>
        /// <param name="name">The name.</param>
        /// <exception cref="System.Exception">Failed to create options</exception>
        private NativeOptions(NativeApi.sd_opt_t* handle, string name)
        {
            if (handle == null)
                throw new Exception($"Failed to create {name} options");

            _name = name;
            _handle = handle;
        }

        public static NativeOptions CreateContext() => new NativeOptions(NativeApi.sd_ctx_opt_create(), "Context");
        public static NativeOptions CreateImage() => new NativeOptions(NativeApi.sd_img_gen_opt_create(), "Image");
        public static NativeOptions CreateVideo() => new NativeOptions(NativeApi.sd_vid_gen_opt_create(), "Video");
        public static NativeOptions CreateUpscaler() => new NativeOptions(NativeApi.sd_upscaler_opt_create(), "Upscaler");
        public static NativeOptions CreateUpscale() => new NativeOptions(NativeApi.sd_upscale_opt_create(), "Upscale");
        public static NativeOptions CreateVideoUpscaler() => new NativeOptions(NativeApi.sd_vid_upscaler_opt_create(), "VideoUpscaler");
        public static NativeOptions CreateVideoUpscale() => new NativeOptions(NativeApi.sd_vid_upscale_opt_create(), "VideoUpscale");
        public static NativeOptions CreateAdetailer() => new NativeOptions(NativeApi.sd_adetailer_opt_create(), "Adetailer");
        public static NativeOptions CreateAdetail() => new NativeOptions(NativeApi.sd_adetail_opt_create(), "Adetail");

        /// <summary>
        /// Gets the native handle.
        /// </summary>
        public NativeApi.sd_opt_t* Handle => _handle;


        /// <summary>
        /// Sets a bool value.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        public void SetBool(string key, bool value)
        {
            ThrowIfFailed(NativeApi.sd_opt_set_bool(_handle, key, value), key);
        }


        /// <summary>
        /// Sets a bool value, unless it is the default.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <param name="defaultValue">The default value.</param>
        public void SetBool(string key, bool value, bool defaultValue)
        {
            if (value != defaultValue)
                SetBool(key, value);
        }


        /// <summary>
        /// Sets an int value, values below the minimum are left unset.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <param name="minimum">The minimum the library accepts.</param>
        public void SetInt(string key, long value, long minimum = long.MinValue)
        {
            if (value < minimum)
                return;

            ThrowIfFailed(NativeApi.sd_opt_set_int(_handle, key, value), key);
        }


        /// <summary>
        /// Sets an int value, unless it is the default or below the minimum.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <param name="defaultValue">The default value.</param>
        /// <param name="minimum">The minimum the library accepts.</param>
        public void SetInt(string key, long value, long defaultValue, long minimum)
        {
            if (value != defaultValue)
                SetInt(key, value, minimum);
        }


        /// <summary>
        /// Sets a float value, non-finite values are left unset (automatic).
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        public void SetFloat(string key, float value)
        {
            if (!float.IsFinite(value))
                return;

            ThrowIfFailed(NativeApi.sd_opt_set_float(_handle, key, value), key);
        }


        /// <summary>
        /// Sets a float value, unless it is the default or non-finite.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <param name="defaultValue">The default value.</param>
        public void SetFloat(string key, float value, float defaultValue)
        {
            if (value != defaultValue)
                SetFloat(key, value);
        }


        /// <summary>
        /// Sets a string value, null or empty values are left unset.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        public void SetString(string key, string value)
        {
            if (string.IsNullOrEmpty(value))
                return;

            ThrowIfFailed(NativeApi.sd_opt_set_string(_handle, key, value), key);
        }


        /// <summary>
        /// Sets a float array, null or empty arrays are left unset.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="values">The values.</param>
        public void SetFloats(string key, float[] values)
        {
            if (values.IsNullOrEmpty())
                return;

            fixed (float* valuesPtr = values)
            {
                ThrowIfFailed(NativeApi.sd_opt_set_floats(_handle, key, valuesPtr, (nuint)values.Length), key);
            }
        }


        /// <summary>
        /// Sets an int array, null or empty arrays are left unset.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="values">The values.</param>
        public void SetInts(string key, int[] values)
        {
            if (values.IsNullOrEmpty())
                return;

            fixed (int* valuesPtr = values)
            {
                ThrowIfFailed(NativeApi.sd_opt_set_ints(_handle, key, valuesPtr, (nuint)values.Length), key);
            }
        }


        /// <summary>
        /// Sets a native tensor, the options retain it.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="tensor">The tensor.</param>
        public void SetTensor(string key, NativeApi.sd_tensor_t* tensor)
        {
            ThrowIfFailed(NativeApi.sd_opt_set_tensor(_handle, key, tensor), key);
        }


        /// <summary>
        /// Sets an image, null images are left unset.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="image">The image.</param>
        /// <param name="channels">The channels to send, 1 (mask), 3 (RGB) or 4 (RGBA).</param>
        public void SetImage(string key, ImageTensor image, int channels = 4)
        {
            if (image == null)
                return;

            var tensor = image.ToUnmanaged(channels);
            try
            {
                SetTensor(key, tensor);
            }
            finally
            {
                NativeApi.sd_tensor_free(tensor);
            }
        }


        /// <summary>
        /// Sets a list of images that may differ in size, null or empty arrays are left unset.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="images">The images.</param>
        /// <param name="channels">The channels to send.</param>
        public void SetImages(string key, ImageTensor[] images, int channels = 4)
        {
            if (images.IsNullOrEmpty())
                return;

            var tensors = (NativeApi.sd_tensor_t**)NativeMemory.AllocZeroed((nuint)images.Length, (nuint)sizeof(nint));
            try
            {
                for (int i = 0; i < images.Length; i++)
                {
                    tensors[i] = images[i].ToUnmanaged(channels);
                }
                ThrowIfFailed(NativeApi.sd_opt_set_tensors(_handle, key, tensors, (nuint)images.Length), key);
            }
            finally
            {
                for (int i = 0; i < images.Length; i++)
                {
                    NativeApi.sd_tensor_free(tensors[i]);
                }
                NativeMemory.Free(tensors);
            }
        }


        /// <summary>
        /// Sets equally sized frames as a single video tensor, null or empty arrays are left unset.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="frames">The frames.</param>
        /// <param name="channels">The channels to send.</param>
        public void SetFrames(string key, ImageTensor[] frames, int channels = 4)
        {
            if (frames.IsNullOrEmpty())
                return;

            var tensor = frames.ToUnmanaged(channels);
            try
            {
                SetTensor(key, tensor);
            }
            finally
            {
                NativeApi.sd_tensor_free(tensor);
            }
        }


        /// <summary>
        /// Sets the LoRAs, entries without a path are ignored.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="loras">The loras.</param>
        public void SetLoras(string key, LoraOptions[] loras)
        {
            var validLoras = loras?.Where(x => x != null && !string.IsNullOrEmpty(x.Path)).ToArray();
            if (validLoras.IsNullOrEmpty())
                return;

            var unmanaged = (NativeApi.sd_lora_t*)NativeMemory.AllocZeroed((nuint)validLoras.Length, (nuint)sizeof(NativeApi.sd_lora_t));
            try
            {
                for (int i = 0; i < validLoras.Length; i++)
                {
                    unmanaged[i].is_high_noise = validLoras[i].IsHighNoise;
                    unmanaged[i].multiplier = validLoras[i].Multiplier;
                    unmanaged[i].path = Utf8StringMarshaller.ConvertToUnmanaged(validLoras[i].Path);
                }
                ThrowIfFailed(NativeApi.sd_opt_set_loras(_handle, key, unmanaged, (nuint)validLoras.Length), key);
            }
            finally
            {
                for (int i = 0; i < validLoras.Length; i++)
                {
                    Utf8StringMarshaller.Free(unmanaged[i].path);
                }
                NativeMemory.Free(unmanaged);
            }
        }


        /// <summary>
        /// Sets the embeddings, entries without a name or path are ignored.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="embeddings">The embeddings.</param>
        public void SetEmbeddings(string key, EmbeddingOptions[] embeddings)
        {
            var validEmbeddings = embeddings?.Where(x => x != null && !string.IsNullOrEmpty(x.Name) && !string.IsNullOrEmpty(x.Path)).ToArray();
            if (validEmbeddings.IsNullOrEmpty())
                return;

            var unmanaged = (NativeApi.sd_embedding_t*)NativeMemory.AllocZeroed((nuint)validEmbeddings.Length, (nuint)sizeof(NativeApi.sd_embedding_t));
            try
            {
                for (int i = 0; i < validEmbeddings.Length; i++)
                {
                    unmanaged[i].name = Utf8StringMarshaller.ConvertToUnmanaged(validEmbeddings[i].Name);
                    unmanaged[i].path = Utf8StringMarshaller.ConvertToUnmanaged(validEmbeddings[i].Path);
                }
                ThrowIfFailed(NativeApi.sd_opt_set_embeddings(_handle, key, unmanaged, (nuint)validEmbeddings.Length), key);
            }
            finally
            {
                for (int i = 0; i < validEmbeddings.Length; i++)
                {
                    Utf8StringMarshaller.Free(unmanaged[i].name);
                    Utf8StringMarshaller.Free(unmanaged[i].path);
                }
                NativeMemory.Free(unmanaged);
            }
        }


        /// <summary>
        /// Sets the audio tracks, null or empty arrays are left unset.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="audios">The audios.</param>
        public void SetAudios(string key, AudioTensor[] audios)
        {
            if (audios.IsNullOrEmpty())
                return;

            var unmanaged = (NativeApi.sd_audio_t*)NativeMemory.AllocZeroed((nuint)audios.Length, (nuint)sizeof(NativeApi.sd_audio_t));
            try
            {
                for (int i = 0; i < audios.Length; i++)
                {
                    unmanaged[i] = audios[i].ToUnmanaged();
                }
                ThrowIfFailed(NativeApi.sd_opt_set_audios(_handle, key, unmanaged, (nuint)audios.Length), key);
            }
            finally
            {
                for (int i = 0; i < audios.Length; i++)
                {
                    NativeApi.sd_tensor_free(unmanaged[i].data);
                }
                NativeMemory.Free(unmanaged);
            }
        }


        /// <summary>
        /// Sets the videos, null or empty arrays are left unset.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="videos">The videos.</param>
        public void SetVideos(string key, VideoSequence[] videos)
        {
            if (videos.IsNullOrEmpty())
                return;

            var unmanaged = (NativeApi.sd_video_t*)NativeMemory.AllocZeroed((nuint)videos.Length, (nuint)sizeof(NativeApi.sd_video_t));
            try
            {
                for (int i = 0; i < videos.Length; i++)
                {
                    unmanaged[i].data = videos[i].Frames.ToUnmanaged();
                    unmanaged[i].fps = (int)videos[i].FrameRate;
                    if (videos[i].Audio != null)
                        unmanaged[i].audio = videos[i].Audio.ToUnmanaged();
                }
                ThrowIfFailed(NativeApi.sd_opt_set_videos(_handle, key, unmanaged, (nuint)videos.Length), key);
            }
            finally
            {
                for (int i = 0; i < videos.Length; i++)
                {
                    NativeApi.sd_tensor_free(unmanaged[i].data);
                    NativeApi.sd_tensor_free(unmanaged[i].audio.data);
                }
                NativeMemory.Free(unmanaged);
            }
        }


        /// <summary>
        /// Throws if a setter failed.
        /// </summary>
        /// <param name="status">The status.</param>
        /// <param name="key">The key.</param>
        private void ThrowIfFailed(NativeApi.sd_status_t status, string key)
        {
            NativeApi.ThrowIfFailed(status, $"Failed to set {_name} option '{key}'");
        }


        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (_handle == null)
                return;

            NativeApi.sd_opt_free(_handle);
            _handle = null;
        }
    }
}
