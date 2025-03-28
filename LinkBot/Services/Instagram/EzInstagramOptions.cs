using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace LinkBot.Services.Instagram
{
    public class EzInstagramOptions
    {
        [Required]
        [NotNull]
        public string? ApiKey { get; set; }
    }
}
