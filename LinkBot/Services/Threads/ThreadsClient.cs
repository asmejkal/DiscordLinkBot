using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Nodes;
using HtmlAgilityPack;
using LinkBot.Services.Common;
using LinkBot.Utility;

namespace LinkBot.Services.Threads
{
    public class ThreadsClient : IThreadsClient
    {
        private readonly HttpClient _client;

        public ThreadsClient(HttpClient client)
        {
            _client = client;
        }

        public async Task<ThreadsPost> GetPostAsync(Uri uri, CancellationToken ct)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, uri);
            request.Headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7");
            request.Headers.Add("Accept-Encoding", "gzip, deflate");
            request.Headers.Add("Accept-Language", "en");
            request.Headers.Add("Sec-Ch-Ua", "\"Chromium\";v=\"116\", \"Not)A; Brand\";v=\"24\", \"Google Chrome\";v=\"116\"");
            request.Headers.Add("Sec-Ch-Ua-Mobile", "?0");
            request.Headers.Add("Sec-Ch-Ua-Platform", "\"Windows\"");
            request.Headers.Add("Sec-Fetch-Dest", "document");
            request.Headers.Add("Sec-Fetch-Mode", "navigate");
            request.Headers.Add("Sec-Fetch-Site", "none");
            request.Headers.Add("Sec-Fetch-User", "?1");
            request.Headers.Add("Upgrade-Insecure-Requests", "1");
            request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/116.0.0.0 Safari/537.36");

            var response = await _client.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var gZipStream = new GZipStream(stream, CompressionMode.Decompress);
            using var reader = new StreamReader(gZipStream);
            var html = await reader.ReadToEndAsync();

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var post = doc.DocumentNode
                .Descendants("script")
                .Where(x => x.GetAttributes("data-sjs").Any())
                .Select(x =>
                {
                    try
                    {
                        return JsonSerializer.Deserialize<JsonObject>(x.InnerText.Trim());
                    }
                    catch (JsonException)
                    {
                        return null;
                    }
                })
                .Select(x => x?.FirstDescendantOrDefault((node, key) => key == "post"))
                .FirstOrDefault(x => x is not null)?
                .AsObject();

            if (post is null)
                throw new ArgumentException("Data element not found, possibly not a valid URL", nameof(uri));

            var mediaNodes = post["carousel_media"]?.AsArray().Select(x => x?.AsObject()).WhereNotNull()
                ?? new[] { post };

            var images = mediaNodes
                .Where(x => x.ContainsKey("image_versions2"))
                .Select(x => x?["image_versions2"]?["candidates"]?.AsArray().FirstOrDefault()?["url"]?.GetValue<string>())
                .WhereNotNull();

            var videos = mediaNodes
                .Where(x => x.ContainsKey("video_versions"))
                .Select(x => x?["video_versions"]?.AsArray().Select(x => x?["url"]?.GetValue<string>()).WhereNotNull().FirstOrDefault())
                .WhereNotNull();

            var media = videos.Any() ? videos : images;
            var username = post["user"]?["username"]?.GetValue<string>();
            
            if (username is null || !media.Any())
                throw new ArgumentException("Username or media elements not found, possibly invalid URL", nameof(uri));

            return new(media.Select(x => new MediaItem(new Uri(x), Path.GetFileName(new Uri(x).AbsolutePath))).ToList(), username);
        }
    }
}
