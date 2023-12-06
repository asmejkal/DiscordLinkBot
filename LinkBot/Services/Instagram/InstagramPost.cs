namespace LinkBot.Services.Instagram
{
    public record InstagramPost(IReadOnlyCollection<Uri> MediaUrls, string Username, string? Description, bool HasMore);
}
