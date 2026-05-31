using System.ComponentModel.DataAnnotations;

namespace Nido.Infrastructure.Auth;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required]
    public string Key { get; init; } = string.Empty;
    public string Issuer { get; init; } = "nido-api";
    public string Audience { get; init; } = "nido-clients";
    public int AccessTokenExpiryMinutes { get; init; } = 60;
    public int RefreshTokenExpiryDays { get; init; } = 7;
    public bool SecureCookies { get; init; } = false;
}