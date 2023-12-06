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

        public async Task<LocalAttachment> GetAttachmentAsync(Uri uri, CancellationToken ct)
        {
            if (MediaConverter.IsConvertableFileType(Path.GetExtension(uri.AbsolutePath)))
            {
                using var httpStream = await _client.GetStreamAsync(uri, ct);
                var (stream, path) = await MediaConverter.ConvertAsync(httpStream, uri.AbsolutePath);
                return new LocalAttachment(stream, Path.GetFileName(path));
            }

            return new LocalAttachment(await _client.GetStreamAsync(uri, ct), Path.GetFileName(uri.AbsolutePath));
        }

        public async Task<IReadOnlyCollection<LocalAttachment>> GetAttachmentsAsync(IEnumerable<Uri> uris, CancellationToken ct) =>
            await Task.WhenAll(uris.Select(x => GetAttachmentAsync(x, ct)));
    }
}
