using Qmmands;
using Disqord.Bot.Commands.Application;
using Disqord;
using Microsoft.Extensions.Logging;
using Disqord.Bot.Commands;
using LinkBot.Services.Threads;
using LinkBot.Services.Common;

namespace LinkBot.Modules
{
    public class ThreadsModule : DiscordApplicationModuleBase
    {
        private readonly IThreadsClient _client;
        private readonly IMediaClient _mediaClient;

        public ThreadsModule(IThreadsClient client, IMediaClient mediaClient)
        {
            _client = client;
            _mediaClient = mediaClient;
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

            var attachments = await _mediaClient.GetAttachmentsAsync(post.MediaUrls, Bot.StoppingToken);

            return Response(new LocalInteractionMessageResponse(InteractionResponseType.DeferredMessageUpdate)
                .WithContent(Markdown.Link(Markdown.Bold($"@{post.Username}"), $"<{link}>"))
                .WithAttachments(attachments));
        }
    }
}
