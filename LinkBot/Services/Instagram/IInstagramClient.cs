using System.Collections.Immutable;

namespace LinkBot.Services.Instagram
{
    public interface IInstagramClient
    {
        Task<InstagramPost> GetPostAsync(Uri uri, IImmutableSet<int> mediaPositions, CancellationToken ct);
    }
}