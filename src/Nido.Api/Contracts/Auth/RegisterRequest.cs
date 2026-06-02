using System.ComponentModel.DataAnnotations;

namespace Nido.Api.Contracts.Auth;

public sealed class RegisterRequest
{
    [Required]
    public string Nombre { get; init; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; init; } = string.Empty;

    [Required, RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)[A-Za-z\d@$!%*?&.]{8,}$", ErrorMessage = "Password does not meet complexity requirements.")]
    public string Password { get; init; } = string.Empty;

    [Required]
    public string Sexo { get; init; } = string.Empty;

    public IFormFile? Foto { get; init; }
}
