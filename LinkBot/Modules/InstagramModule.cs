using Qmmands;
using Disqord.Bot.Commands.Application;
using Disqord;
using Microsoft.Extensions.Logging;
using Disqord.Bot.Commands;
using LinkBot.Services.Instagram;
using LinkBot.Utility;
using LinkBot.Services.Common;

namespace LinkBot.Modules
{
    public class InstagramModule : DiscordApplicationModuleBase
    {
        private const int MaxMediaCount = 6;

        private readonly IInstagramClient _client;
        private readonly IMediaClient _mediaClient;

        public InstagramModule(IInstagramClient client, IMediaClient mediaClient)
        {
            _client = client;
            _mediaClient = mediaClient;
        }

        [SlashCommand("instagram")]
        [Description("Embed an Instagram post.")]
        public async ValueTask<IResult> EmbedInstagramAsync(
            [Description("Link to an Instagram post.")]
            string link,
            [Description("Show description.")]
            bool showDescription = false,
            [Description("Select images from the post by typing their position number, separated by colons or spaces.")]
            string selectedImages = "",
            [Description("Exclude images from the post by typing their position number, separated by colons or spaces.")]
            string excludedImages = "")
        {
            await Deferral();

            InstagramPost post;
            try
            {
                post = await _client.GetPostAsync(new Uri(link), CommandArgumentParsers.ParseMediaPositions(selectedImages, excludedImages, MaxMediaCount), Bot.StoppingToken);
            }
            catch (ArgumentException ex)
            {
                Logger.Log(LogLevel.Information, ex, "Failed to open the Instagram post, possibly incorrect URL: {url}", link);
                return Response(new LocalInteractionMessageResponse(InteractionResponseType.DeferredMessageUpdate)
                    .WithContent("Couldn't find any media. Please make sure the link is a valid Instagram post."))
                    .DeleteAfter(TimeSpan.FromSeconds(3));
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, ex, "Failed to open Instagram post {url}", link);
                return Response(new LocalInteractionMessageResponse(InteractionResponseType.DeferredMessageUpdate)
                    .WithContent("Oops... seems that something went wrong."))
                    .DeleteAfter(TimeSpan.FromSeconds(3));
            }

            var attachments = await _mediaClient.GetAttachmentsAsync(post.MediaItems, Bot.StoppingToken);
            
            var response = new LocalInteractionMessageResponse(InteractionResponseType.DeferredMessageUpdate)
                .WithContent(FormatContent(post, link, showDescription))
                .WithAttachments(attachments);

            if (post.HasMore)
                response.WithComponents(new LocalRowComponent().AddComponent(new LocalLinkButtonComponent().WithLabel("See more").WithUrl(link)));

            return Response(response);
        }

        private static string FormatContent(InstagramPost post, string link, bool showDescription)
        {
            if (showDescription && !string.IsNullOrEmpty(post.Description))
                return $"<:ig:725481240245043220> {Markdown.Link(Markdown.Bold(post.Username), $"<{link}>")}\n> " + string.Join("\n> ", post.Description.Split('\n'));
            else
                return $"<:ig:725481240245043220> {Markdown.Link(Markdown.Bold(post.Username), $"<{link}>")}";
        }
    }
}
