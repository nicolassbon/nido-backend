namespace Nido.Application.Preferencias;

public sealed record UpdateUserPreferencesCommand(Guid UsuarioId, int DiasAlerta);
