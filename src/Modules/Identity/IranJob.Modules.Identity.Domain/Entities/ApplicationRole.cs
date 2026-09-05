using Microsoft.AspNetCore.Identity;

namespace IranJob.Modules.Identity.Domain.Entities;

public sealed class ApplicationRole : IdentityRole<Guid>
{
    public ApplicationRole()
    {
    }

    public ApplicationRole(string roleName) : base(roleName)
    {
    }
}
