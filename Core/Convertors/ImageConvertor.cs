using System;
using System.IO;
using ImageMagick;

namespace Core.Convertors
{
    public class ImageConvertor
    {
        public void Resize(string inputImagePath, string outputImagePath, int newWidth)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(inputImagePath);
            ArgumentException.ThrowIfNullOrWhiteSpace(outputImagePath);

            if (newWidth <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(newWidth), "Image width must be greater than zero.");
            }

            if (!File.Exists(inputImagePath))
            {
                throw new FileNotFoundException("The source image was not found.", inputImagePath);
            }

            using var image = new MagickImage(inputImagePath);

            if (image.Width == 0 || image.Height == 0)
            {
                throw new InvalidDataException("The source image has invalid dimensions.");
            }

            // Respect EXIF orientation before calculating the resized output.
            image.AutoOrient();

            // A zero height preserves the original aspect ratio.
            image.Resize((uint)newWidth, 0);

            // Keep the output encoding consistent with the filename extension.
            // JPEG thumbnails retain the previous quality setting.
            var extension = Path.GetExtension(outputImagePath);
            if (string.Equals(extension, ".jpg", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(extension, ".jpeg", StringComparison.OrdinalIgnoreCase))
            {
                image.Quality = 50;
            }

            image.Write(outputImagePath);
        }
    }
}
