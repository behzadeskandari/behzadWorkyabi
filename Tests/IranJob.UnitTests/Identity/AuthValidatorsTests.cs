using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using IranJob.Modules.Identity.Application.Abstractions;
using IranJob.Modules.Identity.Application.Validators;
using IranJob.Modules.Identity.Domain.Constants;
using Xunit;

namespace IranJob.UnitTests.Identity;

public class AuthValidatorsTests
{
    private readonly RegisterRequestValidator _registerValidator = new();
    private readonly LoginRequestValidator _loginValidator = new();

    [Fact]
    public void RegisterRequestValidator_ValidCandidate_ShouldPass()
    {
        // Arrange
        var request = new RegisterRequest(
            "Behzad",
            "Eskandari",
            "behzad@example.com",
            "09123456789",
            "Password123!",
            IdentityRoles.Candidate);

        // Act
        var result = _registerValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void RegisterRequestValidator_ValidEmployer_ShouldPass()
    {
        // Arrange
        var request = new RegisterRequest(
            "Company",
            "Name",
            "company@example.com",
            "09123456789",
            "Password123!",
            IdentityRoles.Employer);

        // Act
        var result = _registerValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void RegisterRequestValidator_MissingFirstName_ShouldFail()
    {
        // Arrange
        var request = new RegisterRequest(
            "",
            "Eskandari",
            "behzad@example.com",
            "09123456789",
            "Password123!",
            IdentityRoles.Candidate);

        // Act
        var result = _registerValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "FirstName");
    }

    [Fact]
    public void RegisterRequestValidator_MissingLastName_ShouldFail()
    {
        // Arrange
        var request = new RegisterRequest(
            "Behzad",
            "",
            "behzad@example.com",
            "09123456789",
            "Password123!",
            IdentityRoles.Candidate);

        // Act
        var result = _registerValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "LastName");
    }

    [Fact]
    public void RegisterRequestValidator_InvalidEmail_ShouldFail()
    {
        // Arrange
        var request = new RegisterRequest(
            "Behzad",
            "Eskandari",
            "invalid-email",
            "09123456789",
            "Password123!",
            IdentityRoles.Candidate);

        // Act
        var result = _registerValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Fact]
    public void RegisterRequestValidator_InvalidPhone_ShouldFail()
    {
        // Arrange
        var request = new RegisterRequest(
            "Behzad",
            "Eskandari",
            "behzad@example.com",
            "123456789",
            "Password123!",
            IdentityRoles.Candidate);

        // Act
        var result = _registerValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PhoneNumber");
    }

    [Fact]
    public void RegisterRequestValidator_WeakPassword_ShouldFail()
    {
        // Arrange
        var request = new RegisterRequest(
            "Behzad",
            "Eskandari",
            "behzad@example.com",
            "09123456789",
            "weak",
            IdentityRoles.Candidate);

        // Act
        var result = _registerValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password");
    }

    [Fact]
    public void RegisterRequestValidator_AdminRole_ShouldFail()
    {
        // Arrange
        var request = new RegisterRequest(
            "Behzad",
            "Eskandari",
            "behzad@example.com",
            "09123456789",
            "Password123!",
            IdentityRoles.Admin);

        // Act
        var result = _registerValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Role");
    }

    [Fact]
    public void RegisterRequestValidator_SuperAdminRole_ShouldFail()
    {
        // Arrange
        var request = new RegisterRequest(
            "Behzad",
            "Eskandari",
            "behzad@example.com",
            "09123456789",
            "Password123!",
            IdentityRoles.SuperAdmin);

        // Act
        var result = _registerValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Role");
    }

    [Fact]
    public void RegisterRequestValidator_RecruiterRole_ShouldFail()
    {
        // Arrange
        var request = new RegisterRequest(
            "Behzad",
            "Eskandari",
            "behzad@example.com",
            "09123456789",
            "Password123!",
            IdentityRoles.Recruiter);

        // Act
        var result = _registerValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Role");
    }

    [Fact]
    public void LoginRequestValidator_ValidRequest_ShouldPass()
    {
        // Arrange
        var request = new LoginRequest("user@example.com", "Password123!");

        // Act
        var result = _loginValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void LoginRequestValidator_MissingIdentifier_ShouldFail()
    {
        // Arrange
        var request = new LoginRequest("", "Password123!");

        // Act
        var result = _loginValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Identifier");
    }

    [Fact]
    public void LoginRequestValidator_MissingPassword_ShouldFail()
    {
        // Arrange
        var request = new LoginRequest("user@example.com", "");

        // Act
        var result = _loginValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password");
    }
}
