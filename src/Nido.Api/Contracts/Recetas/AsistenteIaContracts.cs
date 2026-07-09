namespace Nido.Api.Contracts.Recetas;

public sealed record AsistenteIaRequest(
    string Pregunta,
    AsistenteIaRecetaContext? Receta,
    IReadOnlyList<string>? Alacena,
    IReadOnlyList<AsistenteIaMensaje>? Historial);

public sealed record AsistenteIaRecetaContext(
    Guid? Id,
    string Nombre,
    IReadOnlyList<string> Ingredientes,
    IReadOnlyList<string> Faltantes);

public sealed record AsistenteIaMensaje(
    string Role,
    string Text);

public sealed record AsistenteIaResponse(string Respuesta);
