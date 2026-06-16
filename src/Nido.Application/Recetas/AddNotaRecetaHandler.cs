namespace Nido.Application.Recetas;

public sealed record AddNotaRecetaCommand(
    Guid   RecetaId,
    Guid   HogarId,
    Guid   UsuarioId,
    string Texto);

public sealed class AddNotaRecetaHandler
{
    private const int MaxTextoLength = 1000;
    private readonly INotaRecetaRepository _repository;

    public AddNotaRecetaHandler(INotaRecetaRepository repository)
    {
        _repository = repository;
    }

    public async Task<NotaItem> Handle(AddNotaRecetaCommand command, CancellationToken ct)
    {
        var texto = command.Texto?.Trim() ?? string.Empty;

        if (string.IsNullOrEmpty(texto))
            throw new ArgumentException("El texto de la nota es requerido.", nameof(command));

        if (texto.Length > MaxTextoLength)
            throw new ArgumentException($"La nota no puede exceder {MaxTextoLength} caracteres.", nameof(command));

        return await _repository.AddAsync(command.RecetaId, command.HogarId, command.UsuarioId, texto, ct);
    }
}
