using IranJob.Modules.ReferenceData.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IranJob.Modules.ReferenceData.Infrastructure.Persistence.Configurations;

public sealed class JobCategoryConfiguration : IEntityTypeConfiguration<JobCategory>
{
    public void Configure(EntityTypeBuilder<JobCategory> builder)
    {
        builder.ToTable("JobCategories", "reference_data");
        builder.HasKey(category => category.Id);
        builder.Property(category => category.Name).HasMaxLength(150).IsRequired();
        builder.Property(category => category.NormalizedName).HasMaxLength(150).IsRequired();
        builder.Property(category => category.Description).HasMaxLength(1000);
        builder.HasIndex(category => category.NormalizedName).IsUnique();
        builder.HasIndex(category => category.IsActive);
    }
}
