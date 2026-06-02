namespace Nido.Application.Recetas;

public sealed record CocinarRecetaResult(
    Guid RecetaId,
    int VecesCocinada);
