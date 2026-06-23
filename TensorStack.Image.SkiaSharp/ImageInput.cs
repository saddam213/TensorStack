// Copyright (c) TensorStack. All rights reserved.
// Licensed under the Apache 2.0 License.
using SkiaSharp;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TensorStack.Common;
using TensorStack.Common.Tensor;

namespace TensorStack.Image
{
    /// <summary>
    /// ImageInput implementation with SkiaSharp.SKBitmap.
    /// </summary>
    public class ImageInput : ImageInputBase
    {
        private readonly string _sourceFile;
        private SKBitmap _image;

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageInput"/> class.
        /// </summary>
        /// <param name="tensor">The tensor.</param>
        public ImageInput(ImageTensor tensor) : base(tensor)
        {
            _image = tensor.ToBitmap();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageInput"/> class.
        /// </summary>
        /// <param name="image">The image.</param>
        public ImageInput(SKBitmap image)
            : base(image.ToTensor())
        {
            _image = image;
        }


        /// <summary>
        /// Initializes a new instance of the <see cref="ImageInput"/> class.
        /// </summary>
        /// <param name="filename">The filename.</param>
        public ImageInput(string filename)
            : this(SKBitmap.Decode(filename))
        {
            _sourceFile = filename;
        }


        /// <summary>
        /// Initializes a new instance of the <see cref="ImageInput"/> class.
        /// </summary>
        /// <param name="filename">The filename.</param>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        /// <param name="resizeMode">The resize mode.</param>
        public ImageInput(string filename, int width, int height, ResizeMode resizeMode = ResizeMode.Stretch) : this(filename)
        {
            Resize(width, height, resizeMode);
        }


        /// <summary>
        /// Gets the image.
        /// </summary>
        public SKBitmap Image => _image;

        /// <summary>
        /// Gets the source filename.
        /// </summary>
        public override string SourceFile => _sourceFile;


        /// <summary>
        /// Saves the image.
        /// </summary>
        /// <param name="filename">The filename.</param>
        public override void Save(string filename)
        {
            using (var image = SKImage.FromBitmap(Image))
            using (var data = image.Encode(SKEncodedImageFormat.Png, 100))
            {
                File.WriteAllBytes(filename, data.ToArray());
            }
        }


        /// <summary>
        /// Save the Image to file
        /// </summary>
        /// <param name="filename">The filename.</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>Task.</returns>
        public override async Task SaveAsync(string filename, CancellationToken cancellationToken = default)
        {
            using (var image = SKImage.FromBitmap(Image))
            using (var data = image.Encode(SKEncodedImageFormat.Png, 100))
            {
                await File.WriteAllBytesAsync(filename, data.ToArray(), cancellationToken);
            }
        }


        /// <summary>
        /// Called when Tensor data has changed
        /// </summary>
        protected override void OnTensorDataChanged()
        {
            base.OnTensorDataChanged();
            _image = this.ToBitmap();
        }


        /// <summary>
        /// Releases resources.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            _image?.Dispose();
            _image = null;
            base.Dispose(disposing);
        }

    }
}
