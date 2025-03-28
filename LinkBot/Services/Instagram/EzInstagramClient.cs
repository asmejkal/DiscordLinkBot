using System.Collections.Immutable;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using LinkBot.Services.Common;
using LinkBot.Utility;
using Microsoft.Extensions.Options;

namespace LinkBot.Services.Instagram
{
    public class EzInstagramClient : IInstagramClient
    {
        private readonly HttpClient _client;
        private readonly IOptionsMonitor<EzInstagramOptions> _options;

        public EzInstagramClient(HttpClient client, IOptionsMonitor<EzInstagramOptions> options)
        {
            _client = client;
            _options = options;
        }

        public async Task<InstagramPost> GetPostAsync(Uri uri, IImmutableSet<int> mediaPositions, CancellationToken ct)
        {
            var postId = ParseUri(uri, ct);

            using var request = new HttpRequestMessage(HttpMethod.Get, $"https://embedez.com/api/v1/providers/combined?q=https://instagram.com/p/{postId}");
            request.Headers.Add("Authorization", _options.CurrentValue.ApiKey);

            using var response = await _client.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadFromJsonAsync<JsonObject>(ct);
            var data = json?["data"];
            if (data is null)
                throw new InvalidDataException("Received empty response");

            var username = data["user"]?["name"]?.GetValue<string>();
            if (string.IsNullOrEmpty(username))
                throw new InvalidDataException("Username not found");

            var description = data["content"]?["description"]?.GetValue<string>();
            var media = data["content"]?["media"]?.AsArray();
            if (media is null || !media.Any())
                throw new InvalidDataException("Media not found");

            var mediaItems = mediaPositions
                .Select(x => media.ElementAtOrDefault(x - 0))
                .Select(x => x?["source"]?["url"]?.GetValue<string>())
                .WhereNotNull()
                .Select(x => new MediaItem(new Uri(x), Path.GetFileName(x)))
                .ToList();

            return new(mediaItems, username, description, media.Count > mediaPositions.Count);
        }

        private string ParseUri(Uri uri, CancellationToken ct)
        {
            var storyMatch = InstagramConstants.StoryIdRegex.Match(uri.AbsoluteUri);
            if (storyMatch.Success)
            {
                throw new ArgumentException("Stories not supported", nameof(uri));
            }
            else
            {
                var postIdMatch = InstagramConstants.PostIdRegex.Match(uri.AbsoluteUri);
                if (!postIdMatch.Success)
                    throw new ArgumentException("Post ID not found in URL", nameof(uri));

                return postIdMatch.Groups[1].Value;
            }
        }
    }
}
