namespace Nido.Api.Contracts.UsuariosPerfil;

public sealed record ActualizarPerfilRequest(
    Guid UsuarioId,
    string Nombre,
    string Sexo,
    string? FotoUrl
);
