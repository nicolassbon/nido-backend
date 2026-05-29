namespace Nido.Api.Contracts.Onboarding;

public sealed record RestriccionCatalogoResponse(Guid Id, string Nombre, string Tipo);

public sealed record MetaCatalogoResponse(Guid Id, string Nombre);
