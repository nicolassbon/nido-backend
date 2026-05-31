namespace Nido.Application.Auth.Register;

public sealed record RegisterUserResult(Guid UsuarioId, Guid HogarId, string AccessToken, string? RefreshToken = null);
