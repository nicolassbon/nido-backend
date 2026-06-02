using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nido.Domain.Electrodomesticos;

namespace Nido.Infrastructure.Persistence.Configurations;

public sealed class ElectrodomesticoConfiguration : IEntityTypeConfiguration<Electrodomestico>
{
    public void Configure(EntityTypeBuilder<Electrodomestico> builder)
    {
        builder.ToTable("electrodomesticos");

        builder.HasKey(electrodomestico => electrodomestico.Id);

        builder.Property(electrodomestico => electrodomestico.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(electrodomestico => electrodomestico.HogarId)
            .HasColumnName("hogar_id")
            .IsRequired();

        builder.Property(electrodomestico => electrodomestico.Nombre)
            .HasColumnName("nombre")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(electrodomestico => electrodomestico.Tipo)
            .HasColumnName("tipo")
            .HasMaxLength(100);

        builder.Property(electrodomestico => electrodomestico.Estado)
            .HasColumnName("estado")
            .HasMaxLength(100);
    }
}