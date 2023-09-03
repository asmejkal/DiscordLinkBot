namespace LinkBot.Services.BStage
{
    public interface IBStageClient
    {
        Task<BStagePost> GetPostAsync(Uri uri, CancellationToken ct);
    }
}