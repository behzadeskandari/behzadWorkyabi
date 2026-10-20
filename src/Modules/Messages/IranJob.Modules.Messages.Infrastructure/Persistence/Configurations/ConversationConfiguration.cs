using IranJob.Modules.Messages.Domain.Entities;
using IranJob.Modules.Messages.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IranJob.Modules.Messages.Infrastructure.Persistence.Configurations;

public sealed class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> builder)
    {
        builder.ToTable("Conversations");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Subject).HasMaxLength(280).IsRequired();
        builder.Property(c => c.CreatedAt).IsRequired();

        builder.Property(c => c.LastMessageAt).IsRequired(false);

        builder.HasMany(c => c.Participants)
            .WithOne(c => c.Conversation)
            .HasForeignKey(c => c.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.Messages)
            .WithOne(c => c.Conversation)
            .HasForeignKey(c => c.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(c => c.JobPostingId).HasDatabaseName("IX_Conversations_JobPostingId");
        builder.HasIndex(c => c.JobApplicationId).HasDatabaseName("IX_Conversations_JobApplicationId");

        // Non-unique lookup index: separate explicit create requests may own
        // separate conversations for the same (JobPostingId, JobApplicationId)
        // context, so duplicates are permitted by design.
        builder.HasIndex(c => new { c.JobPostingId, c.JobApplicationId })
            .HasDatabaseName("IX_Conversations_JobPostingId_JobApplicationId");
    }
}
