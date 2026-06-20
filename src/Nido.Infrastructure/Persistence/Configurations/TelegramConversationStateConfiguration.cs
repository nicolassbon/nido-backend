using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nido.Infrastructure.Persistence.Entities;

namespace Nido.Infrastructure.Persistence.Configurations;

public sealed class TelegramConversationStateConfiguration : IEntityTypeConfiguration<TelegramConversationStateEntity>
{
    public void Configure(EntityTypeBuilder<TelegramConversationStateEntity> builder)
    {
        builder.ToTable("telegram_conversation_states");

        builder.HasKey(x => x.ChatId);

        builder.Property(x => x.ChatId)
            .HasColumnName("chat_id");

        builder.Property(x => x.MenuId)
            .HasColumnName("menu_id")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.PayloadJson)
            .HasColumnName("payload_json");

        builder.Property(x => x.LastInteractionAtUtc)
            .HasColumnName("last_interaction_at_utc")
            .HasColumnType("timestamp without time zone");

        builder.Property(x => x.ExpiresAtUtc)
            .HasColumnName("expires_at_utc")
            .HasColumnType("timestamp without time zone");

        builder.HasIndex(x => x.ExpiresAtUtc)
            .HasDatabaseName("ix_telegram_conversation_states_expires_at_utc");
    }
}
