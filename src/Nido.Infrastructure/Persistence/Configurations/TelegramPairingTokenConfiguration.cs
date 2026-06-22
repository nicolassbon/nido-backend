using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nido.Infrastructure.Persistence.Entities;

namespace Nido.Infrastructure.Persistence.Configurations;

public sealed class TelegramPairingTokenConfiguration : IEntityTypeConfiguration<TelegramPairingToken>
{
    public void Configure(EntityTypeBuilder<TelegramPairingToken> builder)
    {
        builder.ToTable("telegram_pairing_tokens");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("uuid_generate_v4()");

        builder.Property(e => e.HogarId).HasColumnName("hogar_id");
        builder.Property(e => e.UsuarioId).HasColumnName("usuario_id");
        builder.Property(e => e.TokenHash)
            .HasColumnName("token_hash")
            .HasMaxLength(255)
            .IsRequired();
        builder.Property(e => e.Status).HasColumnName("status");
        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp without time zone")
            .HasDefaultValueSql("now()");
        builder.Property(e => e.ExpiresAt)
            .HasColumnName("expires_at")
            .HasColumnType("timestamp without time zone");
        builder.Property(e => e.ConsumedAt)
            .HasColumnName("consumed_at")
            .HasColumnType("timestamp without time zone");
        builder.Property(e => e.RevokedAt)
            .HasColumnName("revoked_at")
            .HasColumnType("timestamp without time zone");

        builder.HasIndex(e => e.TokenHash)
            .IsUnique()
            .HasDatabaseName("uq_telegram_pairing_tokens_hash");

        builder.HasOne(e => e.Hogar)
            .WithMany()
            .HasForeignKey(e => e.HogarId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("telegram_pairing_tokens_hogar_id_fkey");

        builder.HasOne(e => e.Usuario)
            .WithMany()
            .HasForeignKey(e => e.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("telegram_pairing_tokens_usuario_id_fkey");
    }
}
