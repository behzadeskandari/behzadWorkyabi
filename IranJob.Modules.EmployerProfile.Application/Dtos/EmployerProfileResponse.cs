using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IranJob.Modules.EmployerProfile.Application.Dtos
{
    public record EmployerProfileResponse(
     Guid Id,
     Guid UserId,
     string CompanyName,
     string? CompanyDescription,
     string Industry,
     string CompanySize,
     string? WebsiteUrl,
     string? LinkedInUrl,
     string? LogoUrl,
     int? FoundedYear,
     string City,
     string Province,
     string? Address,
     string? PostalCode,
     string? ContactEmail,
     string? ContactPhone,
     DateTimeOffset CreatedAt,
     DateTimeOffset? UpdatedAt);
}
