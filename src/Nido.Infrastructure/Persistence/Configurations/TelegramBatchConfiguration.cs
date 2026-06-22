using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nido.Infrastructure.Persistence.Entities;

namespace Nido.Infrastructure.Persistence.Configurations;

public sealed class TelegramBatchConfiguration : IEntityTypeConfiguration<TelegramBatch>
{
    public void Configure(EntityTypeBuilder<TelegramBatch> builder)
    {
        builder.ToTable("telegram_batches", t =>
        {
            t.HasCheckConstraint(
                "ck_telegram_batches_status",
                "status >= 0 AND status <= 4");
        });

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("uuid_generate_v4()");

        builder.Property(e => e.Status).HasColumnName("status");
        builder.Property(e => e.Attempts)
            .HasColumnName("attempts")
            .HasDefaultValue(0);
        builder.Property(e => e.NextAttemptAt)
            .HasColumnName("next_attempt_at")
            .HasColumnType("timestamp without time zone");
        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp without time zone")
            .HasDefaultValueSql("now()");
    }
}
