using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace IranJob.Modules.EmployerProfile.Infrastructure.Persistence
{
    public class EmployerDbContext : DbContext
    {
        public EmployerDbContext(DbContextOptions<EmployerDbContext> options) : base(options)
        {
        }

        public DbSet<EmployerProfile.Domain.Entities.EmployerProfile> EmployerProfiles => Set<EmployerProfile.Domain.Entities.EmployerProfile>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(EmployerDbContext).Assembly);
        }
    }
}
