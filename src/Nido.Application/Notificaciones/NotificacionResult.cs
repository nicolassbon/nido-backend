using System;

namespace Nido.Application.Notificaciones;

public sealed record NotificacionResult(
    Guid Id,
    Guid UsuarioId,
    string? Tipo,
    string? Mensaje,
    bool Leida,
    Guid? ReferenciaId,
    string? ReferenciaTipo,
    DateTime CreatedAt);
