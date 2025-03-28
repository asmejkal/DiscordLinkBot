using System.Collections.Immutable;

namespace LinkBot.Services.Twitter
{
    public interface ITwitterClient
    {
        Task<TwitterPost> GetPostAsync(Uri uri, CancellationToken ct);
    }
}