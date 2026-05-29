using System.ComponentModel.DataAnnotations;

namespace Nido.Api.Contracts.Auth;

public sealed record RegisterRequest(
    [Required] string Nombre,
    [Required, EmailAddress] string Email,
    [Required, RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)[A-Za-z\d@$!%*?&.]{8,}$", ErrorMessage = "Password does not meet complexity requirements.")] string Password,
    [Required] string Sexo,
    string? FotoUrl);
