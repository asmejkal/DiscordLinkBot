using LinkBot.Services.Common;

namespace LinkBot.Services.Instagram
{
    public record InstagramPost(IReadOnlyCollection<MediaItem> MediaItems, string Username, string? Description, bool HasMore);
}
