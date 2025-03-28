using Disqord;

namespace LinkBot.Services.Common
{
    public interface IMediaClient
    {
        Task<LocalAttachment> GetAttachmentAsync(MediaItem item, CancellationToken ct);
        Task<IReadOnlyCollection<LocalAttachment>> GetAttachmentsAsync(IEnumerable<MediaItem> items, CancellationToken ct);
    }
}