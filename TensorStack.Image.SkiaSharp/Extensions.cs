// Copyright (c) Adam Clark. All rights reserved.
// Licensed under the Apache 2.0 License.
using SkiaSharp;
using System.Threading.Tasks;
using TensorStack.Common;
using TensorStack.Common.Tensor;

namespace TensorStack.Image
{
    public static class Extensions
    {
        /// <summary>
        /// Converts ImageTensor to SKBitmap.
        /// </summary>
        /// <param name="imageTensor">The image tensor.</param>
        /// <returns>SKBitmap.</returns>
        public static SKBitmap ToImage(this ImageTensor imageTensor)
        {
            return imageTensor.ToBitmap();
        }


        /// <summary>
        /// Converts ImageTensorBase to ImageTensor.
        /// </summary>
        /// <param name="imageTensor">The image tensor.</param>
        /// <returns>ImageTensor.</returns>
        public static ImageInput ToImageInput(this ImageTensor imageTensor)
        {
            return new ImageInput(imageTensor);
        }


        /// <summary>
        /// Saves the ImageTensor.
        /// </summary>
        /// <param name="imageTensor">The image tensor.</param>
        /// <param name="filename">The filename.</param>
        /// <returns>Task.</returns>
        public static Task SaveAsync(this ImageTensor imageTensor, string filename)
        {
            return Task.Run(() =>
            {
                using (var image = imageTensor.ToImageInput())
                {
                    image.Save(filename);
                }
            });
        }


        /// <summary>
        /// Saves the ImageInput.
        /// </summary>
        /// <param name="imageTensor">The image tensor.</param>
        /// <param name="filename">The filename.</param>
        /// <returns>Task.</returns>
        public static Task SaveAsync(this ImageInput imageTensor, string filename)
        {
            return Task.Run(() => imageTensor.Save(filename));
        }


        /// <summary>
        /// Converts SKBitmap to Tensor.
        /// </summary>
        /// <param name="bitmap">The SKBitmap.</param>
        /// <returns>Tensor&lt;System.Single&gt;.</returns>
        internal static ImageTensor ToTensor(this SKBitmap bitmap)
        {
            int width = bitmap.Width;
            int height = bitmap.Height;
            var tensor = new ImageTensor(height, width);
            unsafe
            {
                byte* ptr = (byte*)bitmap.GetPixels();

                for (int y = 0; y < height; y++)
                {
                    byte* row = ptr + (y * bitmap.RowBytes);

                    for (int x = 0; x < width; x++)
                    {
                        byte b = row[x * 4 + 0];
                        byte g = row[x * 4 + 1];
                        byte r = row[x * 4 + 2];
                        byte a = row[x * 4 + 3];

                        tensor[0, 3, y, x] = a.NormalizeToFloat();
                        tensor[0, 0, y, x] = r.NormalizeToFloat();
                        tensor[0, 1, y, x] = g.NormalizeToFloat();
                        tensor[0, 2, y, x] = b.NormalizeToFloat();
                    }
                }
            }

            return tensor;
        }


        /// <summary>
        /// Converts Tensor to SKBitmap.
        /// </summary>
        /// <param name="tensor">The tensor.</param>
        /// <returns>SKBitmap.</returns>
        internal static SKBitmap ToBitmap(this ImageTensor tensor)
        {
            var height = tensor.Dimensions[2];
            var width = tensor.Dimensions[3];
            var bitmap = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
            unsafe
            {
                byte* ptr = (byte*)bitmap.GetPixels();
                for (int y = 0; y < height; y++)
                {
                    byte* row = ptr + (y * bitmap.RowBytes);
                    for (int x = 0; x < width; x++)
                    {
                        byte r = tensor[0, 0, y, x].DenormalizeToByte();
                        byte g = tensor[0, 1, y, x].DenormalizeToByte();
                        byte b = tensor[0, 2, y, x].DenormalizeToByte();
                        byte a = tensor[0, 3, y, x].DenormalizeToByte();

                        row[x * 4 + 0] = r;
                        row[x * 4 + 1] = g;
                        row[x * 4 + 2] = b;
                        row[x * 4 + 3] = a;
                    }
                }
            }
            return bitmap;
        }

    }
}
