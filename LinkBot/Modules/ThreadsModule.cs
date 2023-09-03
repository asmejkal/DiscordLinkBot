using Qmmands;
using Disqord.Bot.Commands.Application;
using Disqord;
using Microsoft.Extensions.Logging;
using Disqord.Bot.Commands;
using LinkBot.Services.Threads;

namespace LinkBot.Modules
{
    public class ThreadsModule : DiscordApplicationModuleBase
    {
        private readonly IThreadsClient _client;
        private readonly IHttpClientFactory _httpClientFactory;

        public ThreadsModule(IThreadsClient client, IHttpClientFactory httpClientFactory)
        {
            _client = client;
            _httpClientFactory = httpClientFactory;
        }

        [SlashCommand("threads")]
        [Description("Embed a Threads post.")]
        public async ValueTask<IResult> EmbedThreadsAsync(
            [Description("Link to a Threads post.")]
            string link)
        {
            await Deferral();

            ThreadsPost post;
            try
            {
                post = await _client.GetPostAsync(new Uri(link), Bot.StoppingToken);
            }
            catch (ArgumentException ex)
            {
                Logger.Log(LogLevel.Information, ex, "Failed to open Threads post, possibly incorrect URL: {url}", link);
                return Response(new LocalInteractionMessageResponse(InteractionResponseType.DeferredMessageUpdate)
                    .WithIsEphemeral(true)
                    .WithContent("Couldn't find any images. Please make sure the link is a valid Threads post."))
                    .DeleteAfter(TimeSpan.FromSeconds(3));
            }

            using var httpClient = _httpClientFactory.CreateClient();
            var attachments = await Task.WhenAll(post.MediaUrls.Select(
                async x => new LocalAttachment(await httpClient.GetStreamAsync(x, Bot.StoppingToken), Path.GetFileName(x.AbsolutePath))));

            return Response(new LocalInteractionMessageResponse(InteractionResponseType.DeferredMessageUpdate)
                .WithContent(Markdown.Bold($"@{post.Username}"))
                .WithAttachments(attachments)
                .WithComponents(new LocalRowComponent().AddComponent(new LocalLinkButtonComponent().WithLabel("View post").WithUrl(link))));
        }
    }
}
