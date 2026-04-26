using System.Collections.Immutable;
using HtmlAgilityPack;
using LinkBot.Services.Common;
using LinkBot.Utility;

namespace LinkBot.Services.Instagram
{
    public class VxInstagramClient : IInstagramClient
    {
        private readonly HttpClient _client;

        public VxInstagramClient(HttpClient client)
        {
            _client = client;
        }

        public async Task<InstagramPost> GetPostAsync(Uri uri, IImmutableSet<int> mediaPositions, CancellationToken ct)
        {
            var postId = ParseUri(uri, ct);
            var media = await GetMediaVxAsync(postId, ct);

            var result = mediaPositions
                .Select(x => media.ElementAtOrDefault(x - 1))
                .WhereNotNull();

            if (!media.Any())
                throw new ArgumentException("Media elements not found, possibly invalid URL", nameof(uri));

            return new(result.ToList(), null, null, media.Count > mediaPositions.Count);
        }

        private async Task<IReadOnlyCollection<MediaItem>> GetMediaVxAsync(string postId, CancellationToken ct)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"https://vxinstagram.com/p/{postId}");

            using var response = await _client.SendAsync(request, ct);
            var content = await response.Content.ReadAsStringAsync(ct);

            var doc = new HtmlDocument();
            doc.LoadHtml(content);

            var items = doc.DocumentNode
                .Descendants("a")
                .Where(x => x.GetAttributeValue("download", null) is not null)?
                .Select(x => x.GetAttributeValue("href", null))
                .ToList();

            if (items is null)
                throw new ArgumentException("No items found", nameof(postId));

            return items
                .WhereNotNull()
                .Select(x => new Uri(x))
                .Select((x, i) => new MediaItem(x, $"{postId}-{i}"))
                .ToList();
        }

        private string ParseUri(Uri uri, CancellationToken ct)
        {
            var postIdMatch = InstagramConstants.PostIdRegex.Match(uri.AbsoluteUri);
            if (!postIdMatch.Success)
                throw new ArgumentException("Post ID not found in URL", nameof(uri));

            return postIdMatch.Groups[1].Value;
        }
    }
}
