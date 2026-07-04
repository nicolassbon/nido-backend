using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nido.Infrastructure.Persistence.Entities;

namespace Nido.Infrastructure.Persistence.Configurations;

public sealed class GamificacionNivelDesbloqueadoConfiguration : IEntityTypeConfiguration<GamificacionNivelDesbloqueado>
{
    public void Configure(EntityTypeBuilder<GamificacionNivelDesbloqueado> builder)
    {
        builder.ToTable("gamificacion_niveles_desbloqueados");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();

        builder.Property(e => e.UsuarioId).HasColumnName("usuario_id").IsRequired();
        builder.Property(e => e.Nivel).HasColumnName("nivel").IsRequired();
        builder.Property(e => e.DesbloqueadoEn).HasColumnName("desbloqueado_en")
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.HasIndex(e => new { e.UsuarioId, e.Nivel })
            .IsUnique()
            .HasDatabaseName("uq_gamificacion_niveles_usuario_nivel");

        builder.HasIndex(e => e.UsuarioId)
            .HasDatabaseName("ix_gamificacion_niveles_usuario");

        builder.HasOne(e => e.Usuario)
            .WithMany()
            .HasForeignKey(e => e.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
