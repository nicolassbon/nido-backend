namespace Nido.Infrastructure.Persistence.Entities;

public partial class Factura
{
    public Guid Id { get; set; }
    public Guid HogarId { get; set; }
    public Guid CreadoPor { get; set; }
    public string Nombre { get; set; } = null!;
    public string Tipo { get; set; } = null!;
    public decimal? Monto { get; set; }
    public DateOnly? FechaVencimiento { get; set; }
    public string? ArchivoStorageKey { get; set; }
    public bool Pagada { get; set; }
    public DateTime CreatedAt { get; set; }

    public virtual Hogare Hogar { get; set; } = null!;
    public virtual Usuario CreadoPorNavigation { get; set; } = null!;
}
