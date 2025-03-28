using LinkBot.Services.Common;

namespace LinkBot.Services.Threads
{
    public record ThreadsPost(IReadOnlyCollection<MediaItem> MediaItems, string Username);
}
