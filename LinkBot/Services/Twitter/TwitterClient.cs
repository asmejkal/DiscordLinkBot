using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Web;
using LinkBot.Services.Common;
using LinkBot.Utility;

namespace LinkBot.Services.Twitter
{
    public class TwitterClient : ITwitterClient
    {
        private static readonly Regex PostIdRegex = new(@"https:\/\/(?:www\.)?(?:twitter|x).com\/(\w+)\/status\/(\d+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private readonly HttpClient _client;

        public TwitterClient(HttpClient client)
        {
            _client = client;
        }

        public async Task<TwitterPost> GetPostAsync(Uri uri, CancellationToken ct)
        {
            var (username, postId) = ParseUri(uri);

            try
            {
                var json = await _client.GetFromJsonAsync<JsonObject>($"https://api.fxtwitter.com/u/status/{postId}");
                
                var media = json?["tweet"]?["media"]?["all"]?
                    .AsArray()
                    .Select(x => (Type: x?["type"]?.GetValue<string>(), Url: x?["url"]?.GetValue<string>()))
                    .Select(x => x switch
                    {
                        { Url: null } => null,
                        { Type: "gif" or "video" } => new(new Uri(x.Url), Path.GetFileName(new UriBuilder(x.Url).Path)),
                        _ => ParseImageUri(x.Url)
                    })
                    .WhereNotNull();

                var screenName = json?["tweet"]?["author"]?["screen_name"]?.GetValue<string>();
                var description = json?["tweet"]?["text"]?.GetValue<string>();
                var timestamp = json?["tweet"]?["created_timestamp"]?.GetValue<long>();

                if (timestamp is null || media is null || !media.Any())
                    throw new ArgumentException("Required elements not found, possibly invalid URL", nameof(uri));

                return new(media.ToList(), screenName ?? username, description, DateTimeOffset.FromUnixTimeSeconds(timestamp.Value));
            }
            catch (JsonException ex)
            {
                throw new ArgumentException("Invalid JSON data, possibly invalid URL", nameof(uri), ex);
            }
        }

        private static (string Username, string PostId) ParseUri(Uri uri)
        {
            var postIdMatch = PostIdRegex.Match(uri.AbsoluteUri);
            if (!postIdMatch.Success)
                throw new ArgumentException("Post ID not found in URL", nameof(uri));

            return (postIdMatch.Groups[1].Value, postIdMatch.Groups[2].Value);
        }

        private static MediaItem? ParseImageUri(string uri)
        {
            var uriBuilder = new UriBuilder(uri);
            var extension = Path.GetExtension(uriBuilder.Path);
            var fileName = Path.GetFileName(uriBuilder.Path);

            if (string.IsNullOrEmpty(extension))
                return null;

            var query = HttpUtility.ParseQueryString(uriBuilder.Query);
            query.Set("name", "orig");
            query.Set("format", extension[1..]);

            uriBuilder.Query = query.ToString();
            uriBuilder.Path = uriBuilder.Path[..^extension.Length];

            return new(uriBuilder.Uri, fileName);
        }
    }
}
