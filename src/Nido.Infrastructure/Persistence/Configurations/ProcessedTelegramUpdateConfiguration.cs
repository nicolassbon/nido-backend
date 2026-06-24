using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nido.Infrastructure.Persistence.Entities;

namespace Nido.Infrastructure.Persistence.Configurations;

public sealed class ProcessedTelegramUpdateConfiguration : IEntityTypeConfiguration<ProcessedTelegramUpdate>
{
    public void Configure(EntityTypeBuilder<ProcessedTelegramUpdate> builder)
    {
        builder.ToTable("processed_telegram_updates");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("uuid_generate_v4()");

        builder.Property(e => e.UpdateId).HasColumnName("update_id");
        builder.Property(e => e.UpdateHash)
            .HasColumnName("update_hash")
            .HasMaxLength(255);
        builder.Property(e => e.ProcessedAt)
            .HasColumnName("processed_at")
            .HasColumnType("timestamp without time zone")
            .HasDefaultValueSql("now()");

        builder.HasIndex(e => e.UpdateId)
            .IsUnique()
            .HasDatabaseName("uq_processed_telegram_updates_update_id");
    }
}
