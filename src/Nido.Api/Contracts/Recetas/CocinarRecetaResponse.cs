namespace Nido.Api.Contracts.Recetas;

public sealed record CocinarRecetaResponse(
    Guid RecetaId,
    int VecesCocinada);
