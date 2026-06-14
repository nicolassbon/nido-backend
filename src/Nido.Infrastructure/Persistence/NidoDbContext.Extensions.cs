using Microsoft.EntityFrameworkCore;
using Nido.Infrastructure.Persistence.Entities;

namespace Nido.Infrastructure.Persistence;

// Configures columns that were added after the initial scaffold.
// Keep scaffold-generated NidoDbContext.cs untouched so it can be re-run safely.
public partial class NidoDbContext
{
    public virtual DbSet<ConsumoProducto> ConsumosProducto { get; set; } = null!;

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ConsumoProducto>(entity =>
        {
            entity.ToTable("consumos_producto");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.HogarId).HasColumnName("hogar_id");
            entity.Property(e => e.ProductoId).HasColumnName("producto_id");
            entity.Property(e => e.ProductoNombre).HasColumnName("producto_nombre").HasMaxLength(200).IsRequired();
            entity.Property(e => e.CategoriaId).HasColumnName("categoria_id");
            entity.Property(e => e.Cantidad).HasColumnName("cantidad").HasPrecision(12, 3);
            entity.Property(e => e.UnidadMedida).HasColumnName("unidad_medida").HasMaxLength(20);
            entity.Property(e => e.FechaConsumo).HasColumnName("fecha_consumo");
            entity.Property(e => e.Motivo).HasColumnName("motivo").HasMaxLength(20).IsRequired();
            entity.Property(e => e.UsuarioId).HasColumnName("usuario_id");

            entity.HasIndex(e => new { e.HogarId, e.FechaConsumo })
                .HasDatabaseName("ix_consumos_producto_hogar_fecha");
            entity.HasIndex(e => new { e.HogarId, e.ProductoId, e.FechaConsumo })
                .HasDatabaseName("ix_consumos_producto_hogar_producto_fecha");

            entity.HasOne(e => e.Hogar).WithMany().HasForeignKey(e => e.HogarId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Producto).WithMany().HasForeignKey(e => e.ProductoId).OnDelete(DeleteBehavior.SetNull);
        });

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
