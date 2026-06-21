using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nido.Infrastructure.Persistence.Entities;

namespace Nido.Infrastructure.Persistence.Configurations;

public sealed class TelegramChatLinkConfiguration : IEntityTypeConfiguration<TelegramChatLink>
{
    public void Configure(EntityTypeBuilder<TelegramChatLink> builder)
    {
        builder.ToTable("telegram_chat_links");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("uuid_generate_v4()");

        builder.Property(e => e.ChatId).HasColumnName("chat_id");
        builder.Property(e => e.UsuarioId).HasColumnName("usuario_id");
        builder.Property(e => e.HogarId).HasColumnName("hogar_id");
        builder.Property(e => e.PairedAt)
            .HasColumnName("paired_at")
            .HasColumnType("timestamp without time zone");
        builder.Property(e => e.UnpairedAt)
            .HasColumnName("unpaired_at")
            .HasColumnType("timestamp without time zone");

        builder.HasIndex(e => e.ChatId)
            .IsUnique()
            .HasDatabaseName("uq_telegram_chat_links_active_chat_id")
            .HasFilter("unpaired_at IS NULL");

        builder.HasIndex(e => new { e.UsuarioId, e.HogarId })
            .IsUnique()
            .HasDatabaseName("uq_telegram_chat_links_active_usuario_hogar")
            .HasFilter("unpaired_at IS NULL");

        builder.HasOne(e => e.Usuario)
            .WithMany()
            .HasForeignKey(e => e.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("telegram_chat_links_usuario_id_fkey");

        builder.HasOne(e => e.Hogar)
            .WithMany()
            .HasForeignKey(e => e.HogarId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("telegram_chat_links_hogar_id_fkey");
    }
}
