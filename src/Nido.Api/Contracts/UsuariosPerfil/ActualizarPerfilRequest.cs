using Microsoft.AspNetCore.Http;

namespace Nido.Api.Contracts.UsuariosPerfil;

public sealed record ActualizarPerfilRequest(
    string Nombre,
    string Sexo,
    string? Telefono,
    IFormFile? Foto,
    bool RemoveFoto = false
);
