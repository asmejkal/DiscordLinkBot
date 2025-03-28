using Qmmands;
using Disqord.Bot.Commands.Application;
using Disqord;
using Microsoft.Extensions.Logging;
using Disqord.Bot.Commands;
using LinkBot.Services.Instagram;
using LinkBot.Utility;
using LinkBot.Services.Common;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Immutable;

namespace LinkBot.Modules
{
    public class InstagramModule : DiscordApplicationModuleBase
    {
        private const int MaxMediaCount = 6;

        private readonly IEnumerable<IInstagramClient> _clients;
        private readonly IMediaClient _mediaClient;

        public InstagramModule(
            IEnumerable<IInstagramClient> clients,
            IMediaClient mediaClient)
        {
            _clients = clients;
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

            InstagramPost? post = null;
            foreach (var client in _clients)
            {
                try
                {
                    post = await client.GetPostAsync(new Uri(link), CommandArgumentParsers.ParseMediaPositions(selectedImages, excludedImages, MaxMediaCount), Bot.StoppingToken);
                    if (post is not null)
                        break;
                }
                catch (ArgumentException ex)
                {
                    Logger.Log(LogLevel.Information, ex, "Failed to open the Instagram post, possibly incorrect URL: {Url}", link);
                    return Response(new LocalInteractionMessageResponse(InteractionResponseType.DeferredMessageUpdate)
                        .WithIsEphemeral(true)
                        .WithContent("Couldn't find any media. Please make sure the link is a valid Instagram post."))
                        .DeleteAfter(TimeSpan.FromSeconds(3));
                }
                catch (Exception ex)
                {
                    Logger.Log(LogLevel.Warning, ex, "Failed to get Instagram post {Url} with client {ClientType}", link, client.GetType());
                }
            }

            if (post is null)
            {
                return Response(new LocalInteractionMessageResponse(InteractionResponseType.DeferredMessageUpdate)
                    .WithIsEphemeral(true)
                    .WithContent("Failed to load Instagram post."))
                    .DeleteAfter(TimeSpan.FromSeconds(3));
            }

            var attachments = await _mediaClient.GetAttachmentsAsync(post.MediaUrls, Bot.StoppingToken);
            
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
