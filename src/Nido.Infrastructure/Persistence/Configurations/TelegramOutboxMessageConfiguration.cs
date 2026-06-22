using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nido.Infrastructure.Persistence.Entities;

namespace Nido.Infrastructure.Persistence.Configurations;

public sealed class TelegramOutboxMessageConfiguration : IEntityTypeConfiguration<TelegramOutboxMessage>
{
    public void Configure(EntityTypeBuilder<TelegramOutboxMessage> builder)
    {
        builder.ToTable("telegram_outbox_messages");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("uuid_generate_v4()");

        builder.Property(e => e.HogarId).HasColumnName("hogar_id");
        builder.Property(e => e.ChatId).HasColumnName("chat_id");
        builder.Property(e => e.MessageType)
            .HasColumnName("message_type")
            .HasMaxLength(100)
            .IsRequired();
        builder.Property(e => e.PayloadJson)
            .HasColumnName("payload_json")
            .IsRequired();
        builder.Property(e => e.Status).HasColumnName("status");
        builder.Property(e => e.Attempts)
            .HasColumnName("attempts")
            .HasDefaultValue(0);
        builder.Property(e => e.NextAttemptAt)
            .HasColumnName("next_attempt_at")
            .HasColumnType("timestamp without time zone");
        builder.Property(e => e.LockedUntil)
            .HasColumnName("locked_until")
            .HasColumnType("timestamp without time zone");
        builder.Property(e => e.BatchId).HasColumnName("batch_id");
        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp without time zone")
            .HasDefaultValueSql("now()");

        builder.HasIndex(e => new { e.HogarId, e.ChatId, e.MessageType })
            .IsUnique()
            .HasDatabaseName("uq_telegram_outbox_messages_pending")
            .HasFilter("status = 0");

        builder.HasOne(e => e.Batch)
            .WithMany()
            .HasForeignKey(e => e.BatchId)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("telegram_outbox_messages_batch_id_fkey");
    }
}
