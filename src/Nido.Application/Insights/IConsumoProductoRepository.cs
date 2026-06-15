namespace Nido.Application.Insights;

public interface IConsumoProductoRepository
{
    Task RegistrarAsync(RegistrarConsumoInput input, CancellationToken ct);

    Task<IReadOnlyList<ConsumoPorProducto>> GetConsumosPorProductoAsync(
        Guid hogarId, int diasAtras, CancellationToken ct);

    /// <summary>
    /// Devuelve, para cada producto, las fechas (UTC) en las que se cargó stock
    /// dentro del hogar. Sirve para calcular frecuencia mediana de reposición.
    /// Una "compra" = un registro de stock_hogar creado.
    /// </summary>
    Task<IReadOnlyList<ComprasPorProducto>> GetComprasPorProductoAsync(
        Guid hogarId, int diasAtras, CancellationToken ct);

    /// <summary>
    /// Cocinadas (RecetasCocinadas) agrupadas por día de la semana en el hogar.
    /// Permite descubrir cuál es el día que más se cocina en la casa.
    /// </summary>
    Task<IReadOnlyDictionary<DayOfWeek, int>> GetCocinadasPorDiaSemanaAsync(
        Guid hogarId, int diasAtras, CancellationToken ct);

    /// <summary>Top recetas más cocinadas en el hogar en la ventana.</summary>
    Task<IReadOnlyList<RecetaTopItem>> GetRecetasTopAsync(
        Guid hogarId, int diasAtras, int topN, CancellationToken ct);

    /// <summary>
    /// Stock items abiertos hace más de N días — candidatos a estar olvidados o vencidos.
    /// </summary>
    Task<IReadOnlyList<EnvaseZombieRaw>> GetEnvasesAbiertosLargoAsync(
        Guid hogarId, int diasMinAbierto, CancellationToken ct);

    /// <summary>Última N fechas de cocinada (no agrupado) — para frecuencia global de la casa.</summary>
    Task<IReadOnlyList<DateTime>> GetFechasComprasHogarAsync(
        Guid hogarId, int diasAtras, CancellationToken ct);
}

public sealed record EnvaseZombieRaw(
    Guid StockHogarId,
    string ProductoNombre,
    DateTime UltimaActualizacion);

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

public sealed record ComprasPorProducto(
    Guid? ProductoId,
    string ProductoNombre,
    IReadOnlyList<DateTime> FechasCompra);

public static class ConsumoMotivos
{
    public const string Cocinado = "Cocinado";
    public const string Terminado = "Terminado";
    public const string Vencido = "Vencido";
    public const string Ajuste = "Ajuste";
}
