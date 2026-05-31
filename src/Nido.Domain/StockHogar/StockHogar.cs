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
        Guid usuarioIngresoId)
    {
        Id = Guid.NewGuid();
        HogarId = hogarId;
        ProductoId = productoId;
        Cantidad = cantidad;
        UnidadMedida = unidadMedida;
        FechaVencimiento = fechaVencimiento;
        UsuarioIngresoId = usuarioIngresoId;
    }

    public Guid Id { get; private set; }

    public Guid HogarId { get; private set; }

    public Guid ProductoId { get; private set; }

    public decimal Cantidad { get; private set; }

    public string UnidadMedida { get; private set; } = string.Empty;

    public DateTime? FechaVencimiento { get; private set; }

    public Guid UsuarioIngresoId { get; private set; }
}