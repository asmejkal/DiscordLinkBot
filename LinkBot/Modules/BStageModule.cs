using Qmmands;
using Disqord.Bot.Commands.Application;
using Disqord;
using Microsoft.Extensions.Logging;
using Disqord.Bot.Commands;
using LinkBot.Services.BStage;

namespace LinkBot.Modules
{
    public class BStageModule : DiscordApplicationModuleBase
    {
        private readonly IBStageClient _client;
        private readonly IHttpClientFactory _httpClientFactory;

        public BStageModule(IBStageClient client, IHttpClientFactory httpClientFactory)
        {
            _client = client;
            _httpClientFactory = httpClientFactory;
        }

        [SlashCommand("bstage")]
        [Description("Embed a post from a bstage site.")]
        public async ValueTask<IResult> EmbedBStageAsync(
            [Description("Link to a story feed post.")]
            string link)
        {
            await Deferral();

            BStagePost post;
            try
            {
                post = await _client.GetPostAsync(new Uri(link), Bot.StoppingToken);
            }
            catch (ArgumentException ex)
            {
                Logger.Log(LogLevel.Information, ex, "Failed to open BStage post, possibly incorrect URL: {url}", link);
                return Response(new LocalInteractionMessageResponse(InteractionResponseType.DeferredMessageUpdate)
                    .WithIsEphemeral(true)
                    .WithContent("Couldn't find any images. Please make sure the link is a b.stage website feed post."))
                    .DeleteAfter(TimeSpan.FromSeconds(3));
            }

            using var httpClient = _httpClientFactory.CreateClient();
            var attachments = await Task.WhenAll(post.MediaUrls.Select(
                async x => new LocalAttachment(await httpClient.GetStreamAsync(x, Bot.StoppingToken), Path.GetFileName(x.AbsolutePath))));

            return Response(new LocalInteractionMessageResponse(InteractionResponseType.DeferredMessageUpdate)
                .WithContent(Markdown.Bold($"@{post.AuthorNickname}"))
                .WithAttachments(attachments)
                .WithComponents(new LocalRowComponent().AddComponent(new LocalLinkButtonComponent().WithLabel("View post").WithUrl(link))));
        }
    }
}
