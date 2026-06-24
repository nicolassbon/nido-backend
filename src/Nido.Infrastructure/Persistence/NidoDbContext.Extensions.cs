using Microsoft.EntityFrameworkCore;
using Nido.Infrastructure.Persistence.Configurations;
using Nido.Infrastructure.Persistence.Entities;

namespace Nido.Infrastructure.Persistence;

// Configures columns that were added after the initial scaffold.
// Keep scaffold-generated NidoDbContext.cs untouched so it can be re-run safely.
public partial class NidoDbContext
{
    public virtual DbSet<ConsumoProducto> ConsumosProducto { get; set; } = null!;
    public virtual DbSet<NotaReceta> NotasReceta { get; set; } = null!;
    public virtual DbSet<TelegramChatLink> TelegramChatLinks { get; set; } = null!;
    public virtual DbSet<ProcessedTelegramUpdate> ProcessedTelegramUpdates { get; set; } = null!;
    public virtual DbSet<TelegramOutboxMessage> TelegramOutboxMessages { get; set; } = null!;
    public virtual DbSet<TelegramConversationStateEntity> TelegramConversationStates { get; set; } = null!;
    public virtual DbSet<TelegramBatch> TelegramBatches { get; set; } = null!;
    public virtual DbSet<TelegramPairingToken> TelegramPairingTokens { get; set; } = null!;
    public virtual DbSet<TelegramPairingCode> TelegramPairingCodes { get; set; } = null!;

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new TelegramChatLinkConfiguration());
        modelBuilder.ApplyConfiguration(new ProcessedTelegramUpdateConfiguration());
        modelBuilder.ApplyConfiguration(new TelegramOutboxMessageConfiguration());
        modelBuilder.ApplyConfiguration(new TelegramConversationStateConfiguration());
        modelBuilder.ApplyConfiguration(new TelegramBatchConfiguration());
        modelBuilder.ApplyConfiguration(new TelegramPairingTokenConfiguration());
        modelBuilder.ApplyConfiguration(new TelegramPairingCodeConfiguration());
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

        modelBuilder.Entity<NotaReceta>(entity =>
        {
            entity.ToTable("notas_receta");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.RecetaId).HasColumnName("receta_id");
            entity.Property(e => e.HogarId).HasColumnName("hogar_id");
            entity.Property(e => e.UsuarioId).HasColumnName("usuario_id");
            entity.Property(e => e.Texto).HasColumnName("texto").HasMaxLength(1000).IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");

            entity.HasIndex(e => new { e.RecetaId, e.HogarId }).HasDatabaseName("ix_notas_receta_receta_hogar");

            entity.HasOne(e => e.Receta).WithMany().HasForeignKey(e => e.RecetaId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Hogar).WithMany().HasForeignKey(e => e.HogarId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Usuario).WithMany().HasForeignKey(e => e.UsuarioId).OnDelete(DeleteBehavior.Cascade);
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

        modelBuilder.Entity<ReseniasRecetum>(entity =>
        {
            entity.Property(e => e.HogarId).HasColumnName("hogar_id");
            entity.HasOne(e => e.Hogar)
                .WithMany()
                .HasForeignKey(e => e.HogarId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => new { e.RecetaId, e.HogarId, e.UsuarioId })
                .IsUnique()
                .HasDatabaseName("uq_resenias_receta_hogar_usuario");
        });

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.Property(e => e.AceptaTerminos).HasColumnName("acepta_terminos").HasDefaultValue(false);
            entity.Property(e => e.AceptaTerminosAt).HasColumnName("acepta_terminos_at");
        });
    }
}
