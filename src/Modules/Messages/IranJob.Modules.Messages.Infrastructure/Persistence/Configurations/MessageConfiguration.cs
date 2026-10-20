using IranJob.Modules.Messages.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IranJob.Modules.Messages.Infrastructure.Persistence.Configurations;

public sealed class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.ToTable("Messages");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Content).HasMaxLength(20000).IsRequired();
        builder.Property(m => m.SenderName).HasMaxLength(100).IsRequired();
        builder.Property(m => m.SentAt).IsRequired();

        builder.HasIndex(m => m.ConversationId).HasDatabaseName("IX_Messages_ConversationId");
        builder.HasIndex(m => new { m.ConversationId, m.SentAt }).HasDatabaseName("IX_Messages_ConversationId_SentAt");
        builder.HasIndex(m => m.ReadAt).HasDatabaseName("IX_Messages_ReadAt");

        builder.HasOne(m => m.Conversation)
            .WithMany(c => c.Messages)
            .HasForeignKey(m => m.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
