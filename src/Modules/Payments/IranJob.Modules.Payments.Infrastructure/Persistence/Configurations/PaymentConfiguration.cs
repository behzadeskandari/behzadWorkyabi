using IranJob.Modules.Payments.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IranJob.Modules.Payments.Infrastructure.Persistence.Configurations;

public sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments");
        builder.HasKey(payment => payment.Id);

        builder.Property(payment => payment.EmployerProfileId).IsRequired();
        builder.Property(payment => payment.Amount).IsRequired();
        builder.Property(payment => payment.Currency).HasConversion<string>().HasMaxLength(10).IsRequired();
        builder.Property(payment => payment.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(payment => payment.PaymentMethod).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(payment => payment.Authority).HasMaxLength(200);
        builder.Property(payment => payment.Description).HasMaxLength(500);
        builder.Property(payment => payment.CallbackUrl).HasMaxLength(1000);
        builder.Property(payment => payment.RelatedJobPostingId);
        builder.Property(payment => payment.CreatedAt).IsRequired();
        builder.Property(payment => payment.PaidAt);
        builder.Property(payment => payment.FailedAt);
        builder.Property(payment => payment.FailureReason).HasMaxLength(1000);

        // IsConcurrencyToken (without IsRowVersion) so the column is included in
        // INSERT statements â€” IsRowVersion sets ValueGenerated.OnAddOrUpdate which
        // SQLite cannot auto-generate for byte[] columns.
        builder.Property(payment => payment.RowVersion)
            .IsConcurrencyToken()
            .HasColumnName("RowVersion");

        // Authority is only assigned after a successful gateway request; until then it is
        // empty, so the uniqueness constraint must ignore empty values.
        builder.HasIndex(payment => payment.Authority).IsUnique().HasFilter("[Authority] <> ''");
        builder.HasIndex(payment => payment.RelatedJobPostingId);
        builder.HasIndex(payment => new { payment.EmployerProfileId, payment.CreatedAt });
    }
}

