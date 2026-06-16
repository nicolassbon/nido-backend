namespace Nido.Application.Recetas;

public sealed record UpsertResenaCommand(
    Guid    RecetaId,
    Guid    HogarId,
    Guid    UsuarioId,
    int     Puntuacion,
    string? Comentario);

public sealed class UpsertResenaHandler
{
    private const int MaxComentarioLength = 1000;

    private readonly IResenaRecetaRepository _repository;

    public UpsertResenaHandler(IResenaRecetaRepository repository)
    {
        _repository = repository;
    }

    public async Task<ResenaItem> Handle(UpsertResenaCommand command, CancellationToken ct)
    {
        if (command.RecetaId == Guid.Empty)
            throw new ArgumentException("La receta es requerida.", nameof(command));

        if (command.HogarId == Guid.Empty)
            throw new ArgumentException("El hogar es requerido.", nameof(command));

        if (command.UsuarioId == Guid.Empty)
            throw new ArgumentException("El usuario es requerido.", nameof(command));

        if (command.Puntuacion < 1 || command.Puntuacion > 5)
            throw new ArgumentException("La puntuacion debe estar entre 1 y 5.", nameof(command));

        var comentario = command.Comentario?.Trim();
        if (!string.IsNullOrEmpty(comentario) && comentario.Length > MaxComentarioLength)
            throw new ArgumentException(
                $"El comentario no puede exceder {MaxComentarioLength} caracteres.",
                nameof(command));

        return await _repository.UpsertAsync(
            command.RecetaId,
            command.HogarId,
            command.UsuarioId,
            command.Puntuacion,
            string.IsNullOrEmpty(comentario) ? null : comentario,
            ct);
    }
}
