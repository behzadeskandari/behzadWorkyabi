using IranJob.Modules.Messages.Domain.Entities;
using IranJob.Modules.Messages.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IranJob.Modules.Messages.Infrastructure.Persistence.Configurations;

public sealed class ConversationParticipantConfiguration : IEntityTypeConfiguration<ConversationParticipant>
{
    public void Configure(EntityTypeBuilder<ConversationParticipant> builder)
    {
        builder.ToTable("ConversationParticipants");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.CreatedAt).IsRequired();

        builder.Property(p => p.Role).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.HasIndex(p => new { p.ConversationId, p.Role })
            .HasDatabaseName("IX_ConversationParticipants_ConversationId_Role")
            .IsUnique();

        // Fast lookup: every conversation the authenticated user participates in.
        builder.HasIndex(p => p.IdentityUserId).HasDatabaseName("IX_ConversationParticipants_IdentityUserId");

        builder.HasOne(p => p.Conversation)
            .WithMany(c => c.Participants)
            .HasForeignKey(p => p.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
