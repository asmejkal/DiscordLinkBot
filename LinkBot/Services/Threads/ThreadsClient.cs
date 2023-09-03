using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Nodes;
using HtmlAgilityPack;
using LinkBot.Utility;
using Microsoft.Extensions.Logging;
using static System.Runtime.InteropServices.JavaScript.JSType;

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
                .Where(x => x.InnerText.Contains(".jpg"))
                .Select(x => JsonSerializer.Deserialize<JsonObject>(x.InnerText.Trim()))
                .Select(x => x?.FirstDescendantOrDefault((node, key) => key == "post"))
                .FirstOrDefault(x => x is not null);

            if (post is null)
                throw new ArgumentException("Data element not found, possibly not a valid URL", nameof(uri));

            var username = post["user"]?["username"]?.GetValue<string>();
            var media = post["carousel_media"]?
                .AsArray()
                .Select(x => x?["image_versions2"]?["candidates"]?.AsArray().FirstOrDefault()?["url"]?.GetValue<string>())
                .WhereNotNull();

            if (media is null)
            {
                var video = post["video_versions"]?
                    .AsArray()
                    .Select(x => x?["url"]?.GetValue<string>())
                    .WhereNotNull()
                    .FirstOrDefault();

                if (video is not null)
                    media = new[] { video };
            }

            if (media is null || username is null || !media.Any())
                throw new ArgumentException("Required elements not found, possibly invalid URL", nameof(uri));

            return new(media.Select(x => new Uri(x)).ToList(), username);
        }
    }
}
