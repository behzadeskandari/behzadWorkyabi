using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IranJob.SharedKernel.Entities;

namespace IranJob.Modules.EmployerProfile.Domain.Entities
{
 

        public class EmployerProfile : Entity
        {
            public Guid UserId { get; private set; }
            public string CompanyName { get; private set; } = string.Empty;
            public string? CompanyDescription { get; private set; }
            public string Industry { get; private set; } = string.Empty;
            public string CompanySize { get; private set; } = string.Empty;
            public string? WebsiteUrl { get; private set; }
            public string? LinkedInUrl { get; private set; }
            public string? LogoUrl { get; private set; }
            public int? FoundedYear { get; private set; }
            public string City { get; private set; } = string.Empty;
            public string Province { get; private set; } = string.Empty;
            public string? Address { get; private set; }
            public string? PostalCode { get; private set; }
            public string? ContactEmail { get; private set; }
            public string? ContactPhone { get; private set; }
            public DateTimeOffset CreatedAt { get; private set; }
            public DateTimeOffset? UpdatedAt { get; private set; }

            private EmployerProfile()
            {
                // Required by EF Core
            }

            public static EmployerProfile Create(
                Guid userId,
                string companyName,
                string industry,
                string companySize,
                string city,
                string province,
                string? companyDescription = null,
                string? websiteUrl = null,
                string? linkedInUrl = null,
                string? logoUrl = null,
                int? foundedYear = null,
                string? address = null,
                string? postalCode = null,
                string? contactEmail = null,
                string? contactPhone = null)
            {
                if (userId == Guid.Empty)
                {
                    throw new ArgumentException("UserId cannot be empty.", nameof(userId));
                }

                if (string.IsNullOrWhiteSpace(companyName))
                {
                    throw new ArgumentException("CompanyName is required.", nameof(companyName));
                }

                if (string.IsNullOrWhiteSpace(industry))
                {
                    throw new ArgumentException("Industry is required.", nameof(industry));
                }

                if (string.IsNullOrWhiteSpace(companySize))
                {
                    throw new ArgumentException("CompanySize is required.", nameof(companySize));
                }

                if (string.IsNullOrWhiteSpace(city))
                {
                    throw new ArgumentException("City is required.", nameof(city));
                }

                if (string.IsNullOrWhiteSpace(province))
                {
                    throw new ArgumentException("Province is required.", nameof(province));
                }

                var profile = new EmployerProfile
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    CompanyName = companyName.Trim(),
                    Industry = industry.Trim(),
                    CompanySize = companySize.Trim(),
                    City = city.Trim(),
                    Province = province.Trim(),
                    CompanyDescription = companyDescription?.Trim(),
                    WebsiteUrl = websiteUrl?.Trim(),
                    LinkedInUrl = linkedInUrl?.Trim(),
                    LogoUrl = logoUrl?.Trim(),
                    FoundedYear = foundedYear,
                    Address = address?.Trim(),
                    PostalCode = postalCode?.Trim(),
                    ContactEmail = contactEmail?.Trim(),
                    ContactPhone = contactPhone?.Trim(),
                    CreatedAt = DateTimeOffset.UtcNow
                };

                return profile;
            }

            public void Update(
                string companyName,
                string industry,
                string companySize,
                string city,
                string province,
                string? companyDescription = null,
                string? websiteUrl = null,
                string? linkedInUrl = null,
                string? logoUrl = null,
                int? foundedYear = null,
                string? address = null,
                string? postalCode = null,
                string? contactEmail = null,
                string? contactPhone = null)
            {
                if (string.IsNullOrWhiteSpace(companyName))
                {
                    throw new ArgumentException("CompanyName is required.", nameof(companyName));
                }

                if (string.IsNullOrWhiteSpace(industry))
                {
                    throw new ArgumentException("Industry is required.", nameof(industry));
                }

                if (string.IsNullOrWhiteSpace(companySize))
                {
                    throw new ArgumentException("CompanySize is required.", nameof(companySize));
                }

                if (string.IsNullOrWhiteSpace(city))
                {
                    throw new ArgumentException("City is required.", nameof(city));
                }

                if (string.IsNullOrWhiteSpace(province))
                {
                    throw new ArgumentException("Province is required.", nameof(province));
                }

                CompanyName = companyName.Trim();
                Industry = industry.Trim();
                CompanySize = companySize.Trim();
                City = city.Trim();
                Province = province.Trim();
                CompanyDescription = companyDescription?.Trim();
                WebsiteUrl = websiteUrl?.Trim();
                LinkedInUrl = linkedInUrl?.Trim();
                LogoUrl = logoUrl?.Trim();
                FoundedYear = foundedYear;
                Address = address?.Trim();
                PostalCode = postalCode?.Trim();
                ContactEmail = contactEmail?.Trim();
                ContactPhone = contactPhone?.Trim();
                UpdatedAt = DateTimeOffset.UtcNow;
            }
        }
}
