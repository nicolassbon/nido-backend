using System.ComponentModel.DataAnnotations;

namespace Nido.Api.Contracts.Preferencias;

public sealed record UpdateUserPreferencesRequest(
    [Range(1, 365)] int? DiasAlerta,
    string? TemaPreferido
);
