namespace LinkBot.Services.BStage
{
    public record BStagePost(IReadOnlyCollection<Uri> MediaUrls, string AuthorNickname);
}
