using IranJob.Modules.Identity.Domain.Constants;
using FluentAssertions;

namespace IranJob.UnitTests.Identity;

public class IdentityRolesTests
{
    [Fact]
    public void All_ShouldContainAllRoles()
    {
        // Arrange & Act
        var allRoles = IdentityRoles.All;

        // Assert
        allRoles.Should().Contain(IdentityRoles.Candidate);
        allRoles.Should().Contain(IdentityRoles.Employer);
        allRoles.Should().Contain(IdentityRoles.Recruiter);
        allRoles.Should().Contain(IdentityRoles.Admin);
        allRoles.Should().Contain(IdentityRoles.SuperAdmin);
        allRoles.Should().HaveCount(5);
    }

    [Fact]
    public void PublicRegistrationRoles_ShouldContainOnlyCandidateAndEmployer()
    {
        // Arrange & Act
        var publicRoles = IdentityRoles.PublicRegistrationRoles;

        // Assert
        publicRoles.Should().Contain(IdentityRoles.Candidate);
        publicRoles.Should().Contain(IdentityRoles.Employer);
        publicRoles.Should().NotContain(IdentityRoles.Recruiter);
        publicRoles.Should().NotContain(IdentityRoles.Admin);
        publicRoles.Should().NotContain(IdentityRoles.SuperAdmin);
        publicRoles.Should().HaveCount(2);
    }

    [Fact]
    public void Admin_ShouldNotBeInPublicRegistrationRoles()
    {
        // Arrange & Act
        var publicRoles = IdentityRoles.PublicRegistrationRoles;

        // Assert
        publicRoles.Should().NotContain(IdentityRoles.Admin);
    }

    [Fact]
    public void SuperAdmin_ShouldNotBeInPublicRegistrationRoles()
    {
        // Arrange & Act
        var publicRoles = IdentityRoles.PublicRegistrationRoles;

        // Assert
        publicRoles.Should().NotContain(IdentityRoles.SuperAdmin);
    }

    [Fact]
    public void Recruiter_ShouldNotBeInPublicRegistrationRoles()
    {
        // Arrange & Act
        var publicRoles = IdentityRoles.PublicRegistrationRoles;

        // Assert
        publicRoles.Should().NotContain(IdentityRoles.Recruiter);
    }
}
