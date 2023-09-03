namespace LinkBot.Services.Threads
{
    public interface IThreadsClient
    {
        Task<ThreadsPost> GetPostAsync(Uri uri, CancellationToken ct);
    }
}