using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IranJob.Modules.EmployerProfile.Application.Dtos;
using IranJob.SharedKernel.Results;
using MediatR;

namespace IranJob.Modules.EmployerProfile.Application.Queries
{
    public record CreateEmployerProfileCommand(
       string CompanyName,
       string Industry,
       string CompanySize,
       string City,
       string Province,
       string? CompanyDescription = null,
       string? WebsiteUrl = null,
       string? LinkedInUrl = null,
       string? LogoUrl = null,
       int? FoundedYear = null,
       string? Address = null,
       string? PostalCode = null,
       string? ContactEmail = null,
       string? ContactPhone = null) : IRequest<Result<EmployerProfileResponse>>;
}
