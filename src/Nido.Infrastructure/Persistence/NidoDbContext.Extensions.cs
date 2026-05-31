using Microsoft.EntityFrameworkCore;
using Nido.Infrastructure.Persistence.Entities;

namespace Nido.Infrastructure.Persistence;

// Configures properties not present in the original scaffold.
// StockHogar's new columns (ubicacion, esta_abierto, porcentaje_consumido)
// are already mapped in NidoDbContext.cs. Only Producto.ImagenUrl needs to be
// added here since it was not part of the original scaffold.
public partial class NidoDbContext
{
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Producto>(entity =>
        {
            entity.Property(e => e.ImagenUrl)
                .HasColumnName("imagen_url")
                .HasMaxLength(500);
        });
    }
}
