using System.Collections.Immutable;
using HtmlAgilityPack;
using LinkBot.Utility;

namespace LinkBot.Services.Instagram
{
    public class DdInstagramClient : IInstagramClient
    {
        private readonly HttpClient _client;

        public DdInstagramClient(HttpClient client)
        {
            _client = client;
        }

        public async Task<InstagramPost> GetPostAsync(Uri uri, IImmutableSet<int> mediaPositions, CancellationToken ct)
        {
            var (postId, isStory) = await ParseUriAsync(uri, ct);
            if (isStory)
                mediaPositions = new[] { 1 }.ToImmutableHashSet();

            var overflowPosition = Enumerable.Range(1, mediaPositions.Count + 1).Except(mediaPositions).First();
            
            var mediaTask = GetMediaAsync(postId, mediaPositions, ct);
            var overflowMediaTask = GetMediaAsync(postId, new[] { overflowPosition }, ct);
            var metadata = await GetMetadataAsync(postId, ct);

            var media = await mediaTask;
            if (!media.Any())
                throw new ArgumentException("Username or media elements not found, possibly invalid URL", nameof(uri));

            var overflowMedia = await overflowMediaTask;
            return new(media, metadata.Username, metadata.Description, overflowMedia.Any());
        }

        private async Task<IReadOnlyCollection<Uri>> GetMediaAsync(string postId, IEnumerable<int> mediaPositions, CancellationToken ct)
        {
            var results = await Task.WhenAll(mediaPositions.Select(async x =>
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, $"https://ddinstagram.com/images/{postId}/{x}");
                request.Headers.Add("User-Agent", "bot");

                using var response = await _client.SendAsync(request, ct);
                if (response.StatusCode is System.Net.HttpStatusCode.Found or System.Net.HttpStatusCode.TemporaryRedirect && response.Headers.Location is not null)
                    return response.Headers.Location;
                else
                    return null;
            }));

            return results.WhereNotNull().ToList();
        }

        private async Task<(string Username, string? Description)> GetMetadataAsync(string postId, CancellationToken ct)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"https://ddinstagram.com/p/{postId}/");
            request.Headers.Add("User-Agent", "bot");

            using var response = await _client.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();

            var html = await response.Content.ReadAsStringAsync(ct);
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var metaTags = doc.DocumentNode.Descendants("meta");
            return 
            (
                metaTags.FirstOrDefault(x => x.GetAttributeValue("property", null) == "twitter:title")?.GetAttributeValue("content", null)
                    ?? throw new InvalidDataException("Username missing"),
                metaTags.FirstOrDefault(x => x.GetAttributeValue("property", null) == "og:description")?.GetAttributeValue("content", null)
            );
        }

        private async Task<string> GetStoryPostIdAsync(string username, string storyId, CancellationToken ct)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"https://ddinstagram.com/stories/{username}/{storyId}");
            request.Headers.Add("User-Agent", "bot");

            using var response = await _client.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();

            var html = await response.Content.ReadAsStringAsync(ct);
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var metaTags = doc.DocumentNode.Descendants("meta");
            var relPath = metaTags.FirstOrDefault(x => x.GetAttributeValue("property", null) == "og:image")?.GetAttributeValue("content", null)
                ?? throw new InvalidDataException("Username missing");

            return relPath.Split('/').Reverse().Skip(1).First();
        }

        private async Task<(string PostId, bool IsStory)> ParseUriAsync(Uri uri, CancellationToken ct)
        {
            var storyMatch = InstagramConstants.StoryIdRegex.Match(uri.AbsoluteUri);
            if (storyMatch.Success)
            {
                var postId = await GetStoryPostIdAsync(storyMatch.Groups[1].Value, storyMatch.Groups[2].Value, ct);
                return (postId, true);
            }
            else
            {
                var postIdMatch = InstagramConstants.PostIdRegex.Match(uri.AbsoluteUri);
                if (!postIdMatch.Success)
                    throw new ArgumentException("Post ID not found in URL", nameof(uri));

                return (postIdMatch.Groups[1].Value, false);
            }
        }
    }
}
