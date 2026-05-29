using System.ComponentModel.DataAnnotations;

namespace Nido.Api.Contracts.Auth;

public sealed record GoogleLoginRequest([Required] string IdToken);
