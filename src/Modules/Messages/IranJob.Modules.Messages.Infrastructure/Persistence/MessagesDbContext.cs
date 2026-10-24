using IranJob.Modules.Messages.Domain.Entities;
using IranJob.Modules.Messages.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

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

        // SQLite has no native ordering/comparison support for DateTimeOffset.
        // Keep the SQL Server production model unchanged while using its sortable
        // binary representation in the existing SQLite integration-test provider.
        if (Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
        {
            var converter = new DateTimeOffsetToBinaryConverter();
            foreach (var property in builder.Model.GetEntityTypes()
                         .SelectMany(entity => entity.GetProperties())
                         .Where(property => property.ClrType == typeof(DateTimeOffset)
                                            || property.ClrType == typeof(DateTimeOffset?)))
            {
                property.SetValueConverter(converter);
            }
        }

        base.OnModelCreating(builder);
    }
}
