using System;

namespace Nido.Api.Contracts.Notificaciones;

public sealed record NotificacionResponse(
    Guid Id,
    Guid UsuarioId,
    string? Tipo,
    string? Mensaje,
    bool Leida,
    Guid? ReferenciaId,
    string? ReferenciaTipo,
    DateTime CreatedAt);
