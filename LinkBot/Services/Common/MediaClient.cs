using Disqord;
using LinkBot.Utility;

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
            if (MediaConverter.IsConvertableFileType(Path.GetExtension(item.Filename)))
            {
                using var httpStream = await _client.GetStreamAsync(item.Uri, ct);
                var (stream, filename) = await MediaConverter.ConvertAsync(httpStream, item.Filename);
                return new LocalAttachment(stream, Path.GetFileName(filename));
            }

            return new LocalAttachment(await _client.GetStreamAsync(item.Uri, ct), item.Filename);
        }

        public async Task<IReadOnlyCollection<LocalAttachment>> GetAttachmentsAsync(IEnumerable<MediaItem> items, CancellationToken ct) =>
            await Task.WhenAll(items.Select(x => GetAttachmentAsync(x, ct)));
    }
}
