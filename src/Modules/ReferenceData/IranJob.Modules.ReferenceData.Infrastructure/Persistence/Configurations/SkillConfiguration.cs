using IranJob.Modules.ReferenceData.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IranJob.Modules.ReferenceData.Infrastructure.Persistence.Configurations;

public sealed class SkillConfiguration : IEntityTypeConfiguration<Skill>
{
    public void Configure(EntityTypeBuilder<Skill> builder)
    {
        builder.ToTable("Skills", "reference_data");
        builder.HasKey(skill => skill.Id);
        builder.Property(skill => skill.Name).HasMaxLength(150).IsRequired();
        builder.Property(skill => skill.NormalizedName).HasMaxLength(150).IsRequired();
        builder.Property(skill => skill.Description).HasMaxLength(1000);
        builder.HasIndex(skill => skill.NormalizedName).IsUnique();
        builder.HasIndex(skill => skill.IsActive);
    }
}
