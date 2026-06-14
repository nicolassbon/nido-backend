namespace Nido.Application.Insights;

public interface IConsumoProductoRepository
{
    Task RegistrarAsync(RegistrarConsumoInput input, CancellationToken ct);

    Task<IReadOnlyList<ConsumoPorProducto>> GetConsumosPorProductoAsync(
        Guid hogarId, int diasAtras, CancellationToken ct);
}

public sealed record RegistrarConsumoInput(
    Guid HogarId,
    Guid? ProductoId,
    string ProductoNombre,
    Guid? CategoriaId,
    decimal Cantidad,
    string? UnidadMedida,
    string Motivo,
    Guid? UsuarioId);

public sealed record ConsumoPorProducto(
    Guid? ProductoId,
    string ProductoNombre,
    decimal CantidadTotal,
    int Eventos,
    int VecesVencido,
    int VecesCocinado,
    DateTime UltimoConsumo);

public static class ConsumoMotivos
{
    public const string Cocinado = "Cocinado";
    public const string Terminado = "Terminado";
    public const string Vencido = "Vencido";
    public const string Ajuste = "Ajuste";
}
