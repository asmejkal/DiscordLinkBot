using Disqord;
using FileTypeChecker.Abstracts;
using FileTypeChecker.Types;
using FileTypeChecker;
using LinkBot.Utility;
using System.IO;

namespace LinkBot.Services.Common
{
    internal class MediaClient : IMediaClient
    {
        private readonly HttpClient _client;

        public MediaClient(HttpClient client)
        {
            _client = client;
        }

        public async Task<LocalAttachment> GetAttachmentAsync(MediaItem item, CancellationToken ct)
        {
            var ext = Path.GetExtension(item.Filename);
            if (!string.IsNullOrEmpty(ext) && !MediaConverter.IsConvertableFileType(ext))
            {
                return new LocalAttachment(await _client.GetStreamAsync(item.Uri, ct), item.Filename);
            }
            else if (!string.IsNullOrEmpty(ext) && MediaConverter.IsConvertableFileType(ext))
            {
                using var httpStream = await _client.GetStreamAsync(item.Uri, ct);
                var (stream, filename) = await MediaConverter.ConvertAsync(httpStream, item.Filename);
                return new LocalAttachment(stream, Path.GetFileName(filename));
            }
            else
            {
                using var httpStream = await _client.GetStreamAsync(item.Uri, ct);
                var memoryStream = new MemoryStream();
                await httpStream.CopyToAsync(memoryStream);

                // Get the actual file type
                var fileType = FileTypeValidator.GetFileType(memoryStream);
                ext = fileType.Extension;
                memoryStream.Position = 0;

                if (MediaConverter.IsConvertableFileType(ext))
                {
                    var (stream, filename) = await MediaConverter.ConvertAsync(memoryStream, item.Filename + $".{ext}");
                    return new LocalAttachment(stream, Path.GetFileName(filename));
                }
                else
                {
                    return new LocalAttachment(memoryStream, item.Filename + $".{ext}");
                }
            }
        }

        public async Task<IReadOnlyCollection<LocalAttachment>> GetAttachmentsAsync(IEnumerable<MediaItem> items, CancellationToken ct) =>
            await Task.WhenAll(items.Select(x => GetAttachmentAsync(x, ct)));
    }
}
