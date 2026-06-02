namespace Nido.Application.Recetas;

public sealed record CocinarRecetaCommand(
    Guid RecetaId,
    Guid HogarId,
    Guid UsuarioId);
