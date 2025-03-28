using LinkBot.Services.Common;

namespace LinkBot.Services.BStage
{
    public record BStagePost(IReadOnlyCollection<MediaItem> MediaItems, string AuthorNickname);
}
