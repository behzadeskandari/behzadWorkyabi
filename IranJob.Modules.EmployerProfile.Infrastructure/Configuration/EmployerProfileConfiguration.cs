using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IranJob.Modules.EmployerProfile.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IranJob.Modules.EmployerProfile.Infrastructure.Configuration
{
    public class EmployerProfileConfiguration : IEntityTypeConfiguration<EmployerProfile.Domain.Entities.EmployerProfile>
    {
        public void Configure(EntityTypeBuilder<EmployerProfile.Domain.Entities.EmployerProfile> builder)
        {
            builder.ToTable("EmployerProfiles", "employers");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.UserId)
                .IsRequired();

            builder.HasIndex(x => x.UserId)
                .IsUnique();

            builder.Property(x => x.CompanyName)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(x => x.CompanyDescription)
                .HasMaxLength(2000);

            builder.Property(x => x.Industry)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(x => x.CompanySize)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(x => x.WebsiteUrl)
                .HasMaxLength(500);

            builder.Property(x => x.LinkedInUrl)
                .HasMaxLength(500);

            builder.Property(x => x.LogoUrl)
                .HasMaxLength(500);

            builder.Property(x => x.City)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(x => x.Province)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(x => x.Address)
                .HasMaxLength(500);

            builder.Property(x => x.PostalCode)
                .HasMaxLength(20);

            builder.Property(x => x.ContactEmail)
                .HasMaxLength(150);

            builder.Property(x => x.ContactPhone)
                .HasMaxLength(50);

            builder.Property(x => x.CreatedAt)
                .IsRequired();

            builder.Property(x => x.UpdatedAt);
        }
    }
}
