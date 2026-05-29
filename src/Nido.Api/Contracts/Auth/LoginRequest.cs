using System.ComponentModel.DataAnnotations;

namespace Nido.Api.Contracts.Auth;

public sealed record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password);
