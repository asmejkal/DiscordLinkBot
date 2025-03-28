using LinkBot.Services.Common;

namespace LinkBot.Services.Twitter
{
    public record TwitterPost(IReadOnlyCollection<MediaItem> MediaItems, string Username, string? Description, DateTimeOffset CreatedAt);
}
