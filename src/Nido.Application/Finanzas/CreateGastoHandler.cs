using Nido.Application.Finanzas.Exceptions;

namespace Nido.Application.Finanzas;

public sealed class CreateGastoHandler
{
    private static readonly HashSet<string> CategoriasValidas = ["Comida", "Transporte", "Servicios", "Otros"];

    private readonly IFinanzasRepository _repository;

    public CreateGastoHandler(IFinanzasRepository repository)
    {
        _repository = repository;
    }

    public async Task<GastoResult> Handle(CreateGastoCommand command, CancellationToken ct)
    {
        if (command.Monto <= 0)
            throw new InvalidGastoMontoException();

        if (!DateOnly.TryParse(command.Fecha, out _))
            throw new InvalidGastoFechaException();

        if (!string.IsNullOrWhiteSpace(command.Categoria) && !CategoriasValidas.Contains(command.Categoria))
            throw new InvalidGastoCategoriaException(command.Categoria);

        return await _repository.CreateGastoAsync(command, ct);
    }
}
