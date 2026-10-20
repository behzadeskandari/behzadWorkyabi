using IranJob.Modules.Messages.Domain.Entities;
using IranJob.Modules.Messages.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace IranJob.Modules.Messages.Infrastructure.Persistence;

/// <summary>
/// Entity framework context for the direct-messaging aggregate. Owned by the
/// Messages module; its migration history lives in the <c>messages</c> schema.
/// </summary>
public sealed class MessagesDbContext : DbContext
{
    public MessagesDbContext(DbContextOptions<MessagesDbContext> options) : base(options) { }

    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<ConversationParticipant> ConversationParticipants => Set<ConversationParticipant>();
    public DbSet<Message> Messages => Set<Message>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.ApplyConfigurationsFromAssembly(typeof(MessagesDbContext).Assembly);
        base.OnModelCreating(builder);
    }
}
