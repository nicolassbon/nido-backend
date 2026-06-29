using Nido.Application.Auth.Register;

namespace Nido.Application.UsuariosPerfil;

public sealed record ActualizarPerfilCommand(
    Guid UsuarioId,
    string Nombre,
    string Sexo,
    string? Telefono,
    RegistrationProfileImageUpload? Foto,
    bool RemoveFoto = false
);
