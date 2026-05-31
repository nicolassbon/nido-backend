namespace Nido.Application.UsuariosPerfil;
public sealed record ActualizarPerfilCommand(
    Guid UsuarioId,
    string Nombre,
    string Sexo,
    string? FotoUrl
);