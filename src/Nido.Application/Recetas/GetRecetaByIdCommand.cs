namespace Nido.Application.Recetas;

public sealed record GetRecetaByIdCommand(Guid Id, Guid HogarId);
