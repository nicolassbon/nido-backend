using Microsoft.EntityFrameworkCore;
using Nido.Infrastructure.Persistence.Entities;

namespace Nido.Infrastructure.Persistence;

// Configures columns that were added after the initial scaffold.
// Keep scaffold-generated NidoDbContext.cs untouched so it can be re-run safely.
public partial class NidoDbContext
{
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Nido.Infrastructure.Persistence.Entities.StockHogar>(entity =>
        {
            entity.Property(e => e.Ubicacion)
                .HasColumnName("ubicacion")
                .HasMaxLength(100)
                .HasDefaultValue("Alacena");

            entity.Property(e => e.EstaAbierto)
                .HasColumnName("esta_abierto")
                .HasDefaultValue(false);

            entity.Property(e => e.PorcentajeConsumido)
                .HasColumnName("porcentaje_consumido")
                .HasPrecision(5, 2)
                .HasDefaultValue(0m);
        });

        modelBuilder.Entity<Producto>(entity =>
        {
            entity.Property(e => e.ImagenUrl)
                .HasColumnName("imagen_url")
                .HasMaxLength(500);
        });
    }
}
