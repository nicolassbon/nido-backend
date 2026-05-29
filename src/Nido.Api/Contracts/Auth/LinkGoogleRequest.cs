using System.ComponentModel.DataAnnotations;

namespace Nido.Api.Contracts.Auth;

public sealed record LinkGoogleRequest([Required] string IdToken);
