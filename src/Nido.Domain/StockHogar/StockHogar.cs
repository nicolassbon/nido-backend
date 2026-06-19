namespace Nido.Domain.StockHogar;

public sealed class StockHogar
{
    private StockHogar()
    {
    }

    public StockHogar(
        Guid hogarId,
        Guid productoId,
        decimal cantidad,
        string unidadMedida,
        DateTime? fechaVencimiento,
        Guid usuarioIngresoId,
        string ubicacion,
        bool estaAbierto,
        decimal porcentajeConsumido,
        int cantidadEnvases = 1)
    {
        if (cantidadEnvases < 1)
            throw new ArgumentOutOfRangeException(nameof(cantidadEnvases), "Debe ser al menos 1.");

        Id = Guid.NewGuid();
        HogarId = hogarId;
        ProductoId = productoId;
        CantidadActual = cantidad;
        UnidadMedida = unidadMedida;
        FechaVencimiento = fechaVencimiento;
        UsuarioIngresoId = usuarioIngresoId;
        Ubicacion = ubicacion;
        EstaAbierto = estaAbierto;
        PorcentajeConsumido = porcentajeConsumido;
        CantidadEnvases = cantidadEnvases;
    }

    public Guid Id { get; private set; }

    public Guid HogarId { get; private set; }

    public Guid ProductoId { get; private set; }

    public decimal CantidadActual { get; private set; }

    public string UnidadMedida { get; private set; } = string.Empty;

    public DateTime? FechaVencimiento { get; private set; }

    public Guid UsuarioIngresoId { get; private set; }

    public string Ubicacion { get; private set; } = string.Empty;

    public bool EstaAbierto { get; private set; }

    public decimal PorcentajeConsumido { get; private set; }

    /// <summary>
    /// Cantidad de envases idénticos del producto. Mínimo 1.
    /// Ej: 2 paquetes de 100g cada uno → CantidadEnvases=2, CantidadActual=100.
    /// </summary>
    public int CantidadEnvases { get; private set; } = 1;
}
