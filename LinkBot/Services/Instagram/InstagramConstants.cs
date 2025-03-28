using System.Text.RegularExpressions;

namespace LinkBot.Services.Instagram
{
    public static class InstagramConstants
    {
        public static readonly Regex PostIdRegex = new(@"https:\/\/(?:www\.)?instagram.com\/(?:p|[^\/]+\/p)\/([\w-_]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        public static readonly Regex StoryIdRegex = new(@"https:\/\/(?:www\.)?instagram.com\/stories\/([\w-_\.]+)\/(\d+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    }
}
