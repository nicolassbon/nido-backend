namespace Nido.Api.Contracts.Hogares;

public sealed record MiembroResponse(Guid UsuarioId, string Nombre, string? Email, string? Rol, string? FotoUrl);
