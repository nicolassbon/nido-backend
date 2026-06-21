namespace Nido.Api.Contracts.Recetas;

public sealed record NotaResponse(
    Guid     Id,
    Guid     RecetaId,
    Guid     HogarId,
    Guid     UsuarioId,
    string   UsuarioNombre,
    string?  UsuarioFotoUrl,
    string   Texto,
    DateTime CreatedAt
);

public sealed record NotasRecetaResponse(IReadOnlyList<NotaResponse> Items);

public sealed record AddNotaRequest(string Texto);
