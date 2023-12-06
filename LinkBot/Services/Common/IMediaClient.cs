using Disqord;

namespace LinkBot.Services.Common
{
    public interface IMediaClient
    {
        Task<LocalAttachment> GetAttachmentAsync(Uri uri, CancellationToken ct);
        Task<IReadOnlyCollection<LocalAttachment>> GetAttachmentsAsync(IEnumerable<Uri> uris, CancellationToken ct);
    }
}