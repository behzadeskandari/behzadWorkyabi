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
    public record GetEmployerProfileQuery : IRequest<Result<EmployerProfileResponse>>;
}
