using Qmmands;
using Disqord.Bot.Commands.Application;
using Disqord;
using Microsoft.Extensions.Logging;
using Disqord.Bot.Commands;
using LinkBot.Services.BStage;
using LinkBot.Services.Common;

namespace LinkBot.Modules
{
    public class BStageModule : DiscordApplicationModuleBase
    {
        private readonly IBStageClient _client;
        private readonly IMediaClient _mediaClient;

        public BStageModule(IBStageClient client, IMediaClient mediaClient)
        {
            _client = client;
            _mediaClient = mediaClient;
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
                    .WithContent("Couldn't find any images. Please make sure the link is a b.stage website feed post."))
                    .DeleteAfter(TimeSpan.FromSeconds(3));
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, ex, "Failed to open BStage post {url}", link);
                return Response(new LocalInteractionMessageResponse(InteractionResponseType.DeferredMessageUpdate)
                    .WithContent("Oops... seems that something went wrong."))
                    .DeleteAfter(TimeSpan.FromSeconds(3));
            }

            var attachments = await _mediaClient.GetAttachmentsAsync(post.MediaItems, Bot.StoppingToken);

            return Response(new LocalInteractionMessageResponse(InteractionResponseType.DeferredMessageUpdate)
                .WithContent(Markdown.Link(Markdown.Bold($"@{post.AuthorNickname}"), $"<{link}>"))
                .WithAttachments(attachments));
        }
    }
}
