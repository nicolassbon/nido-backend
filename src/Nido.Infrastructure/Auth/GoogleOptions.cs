using System.ComponentModel.DataAnnotations;

namespace Nido.Infrastructure.Auth;

public sealed class GoogleOptions
{
    public const string SectionName = "Google";

    [Required]
    public string ClientId { get; init; } = string.Empty;
}
