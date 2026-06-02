namespace Nido.Application.Onboarding;

public sealed record RestriccionCatalogoResult(Guid Id, string Nombre, string Tipo);

public sealed record MetaCatalogoResult(Guid Id, string Nombre);
