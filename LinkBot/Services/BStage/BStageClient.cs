using System.Text.Json;
using System.Text.Json.Nodes;
using HtmlAgilityPack;
using LinkBot.Services.Common;
using LinkBot.Utility;

namespace LinkBot.Services.BStage
{
    public class BStageClient : IBStageClient
    {
        private readonly HttpClient _client;

        public BStageClient(HttpClient client)
        {
            _client = client;
        }

        public async Task<BStagePost> GetPostAsync(Uri uri, CancellationToken ct)
        {
            var html = await _client.GetStringAsync(uri, ct);

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var data = doc.DocumentNode
                .Descendants("script")
                .SingleOrDefault(x => x.GetAttributeValue("id", null) == "__NEXT_DATA__")?
                .InnerText
                .Trim();

            if (data is null)
                throw new ArgumentException("BStage data element not found, possibly not a BStage URL", nameof(uri));

            try
            {
                var json = JsonSerializer.Deserialize<JsonObject>(data);
                var post = json?["props"]?["pageProps"]?["post"];
                var images = post?["images"]?.AsArray().Select(x => x?.GetValue<string>()).WhereNotNull();
                var authorNickname = post?["author"]?["nickname"]?.GetValue<string>();

                if (images is null || authorNickname is null || !images.Any())
                    throw new ArgumentException("Required elements not found, possibly invalid URL", nameof(uri));

                return new(images.Select(x => new MediaItem(new Uri(x), Path.GetFileName(new Uri(x).AbsolutePath))).ToList(), authorNickname);
            }
            catch (JsonException ex)
            {
                throw new ArgumentException("Invalid JSON data, possibly invalid URL", nameof(uri), ex);
            }
        }
    }
}
