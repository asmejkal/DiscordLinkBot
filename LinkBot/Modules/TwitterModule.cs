using Qmmands;
using Disqord.Bot.Commands.Application;
using Disqord;
using Microsoft.Extensions.Logging;
using Disqord.Bot.Commands;
using LinkBot.Services.Twitter;
using LinkBot.Services.Common;

namespace LinkBot.Modules
{
    public class TwitterModule : DiscordApplicationModuleBase
    {
        private readonly ITwitterClient _client;
        private readonly IMediaClient _mediaClient;

        public TwitterModule(ITwitterClient client, IMediaClient mediaClient)
        {
            _client = client;
            _mediaClient = mediaClient;
        }

        [SlashCommand("twitter")]
        [Description("Embed a Twitter post.")]
        public async ValueTask<IResult> EmbedTwitterAsync(
            [Description("Link to a Twitter post.")]
            string link,
            [Description("Show description.")]
            bool showDescription = false)
        {
            await Deferral();

            TwitterPost post;
            try
            {
                post = await _client.GetPostAsync(new Uri(link), Bot.StoppingToken);
            }
            catch (ArgumentException ex)
            {
                Logger.Log(LogLevel.Information, ex, "Failed to open the Twitter post, possibly incorrect URL: {url}", link);
                return Response(new LocalInteractionMessageResponse(InteractionResponseType.DeferredMessageUpdate)
                    .WithContent("Couldn't find any media. Please make sure the link is a valid Twitter post."))
                    .DeleteAfter(TimeSpan.FromSeconds(3));
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, ex, "Failed to open Twitter post {url}", link);
                return Response(new LocalInteractionMessageResponse(InteractionResponseType.DeferredMessageUpdate)
                    .WithContent("Oops... seems that something went wrong."))
                    .DeleteAfter(TimeSpan.FromSeconds(3));
            }

            var attachments = await _mediaClient.GetAttachmentsAsync(post.MediaItems, Bot.StoppingToken);
            
            var response = new LocalInteractionMessageResponse(InteractionResponseType.DeferredMessageUpdate)
                .WithContent(FormatContent(post, link, showDescription))
                .WithAttachments(attachments);

            return Response(response);
        }

        private static string FormatContent(TwitterPost post, string link, bool showDescription)
        {
            var result = $"<:x_logo:1187481692781428756> {Markdown.Link(Markdown.Bold(post.Username), $"<{link}>")} <t:{post.CreatedAt.ToUnixTimeSeconds()}:d>";
            if (showDescription && !string.IsNullOrEmpty(post.Description))
                result += "\n> " + string.Join("\n> ", post.Description.Split('\n'));

            return result;
        }
    }
}
