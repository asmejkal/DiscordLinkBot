using ImageMagick;

namespace LinkBot.Utility
{
    internal static class MediaConverter
    {
        public static bool IsConvertableFileType(string extension) => 
            IsConvertableImageType(extension);

        public static bool IsConvertableImageType(string extension) => extension.ToLowerInvariant() switch
        {
            ".heic" => true,
            _ => false
        };

        public static async Task<(Stream Data, string Path)> ConvertAsync(Stream stream, string path)
        {
            if (IsConvertableImageType(Path.GetExtension(path)))
            {
                var image = new MagickImage(stream);
                image.Format = MagickFormat.Png;

                var data = new MemoryStream();
                await image.WriteAsync(data);
                data.Position = 0;

                return (data, Path.ChangeExtension(path, "png"));
            }

            throw new ArgumentException("Not a convertable file type", nameof(path));
        }
    }
}
