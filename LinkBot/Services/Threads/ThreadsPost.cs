namespace LinkBot.Services.Threads
{
    public record ThreadsPost(IReadOnlyCollection<Uri> MediaUrls, string Username);
}
