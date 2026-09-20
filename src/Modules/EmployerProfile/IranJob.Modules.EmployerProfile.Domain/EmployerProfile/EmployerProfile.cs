using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IranJob.SharedKernel.Entities;

namespace IranJob.Modules.EmployerProfile.Domain.EmployerProfile
{
    public class EmployerProfile : Entity
    {
        public Guid UserId { get; set; }

        public string JobTitle { get; set; }
        public string Biography { get; set; }
        public string Province { get; set; }
        public string City { get; set; }
        public string LinkedInUrl { get; set; }
        public string PortfolioUrl { get; set; }
        
    }

}
