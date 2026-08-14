using FluentAssertions;
using IranJob.SharedKernel;
using IranJob.SharedKernel.Results;

namespace IranJob.UnitTests.SharedKernel;

public class GuardTests
{
    [Fact]
    public void AgainstNullOrWhiteSpace_ReturnsValue_WhenValid()
    {
        var result = Guard.AgainstNullOrWhiteSpace("IranJob", "name");

        result.Should().Be("IranJob");
    }

    [Fact]
    public void AgainstNullOrWhiteSpace_Throws_WhenEmpty()
    {
        var action = () => Guard.AgainstNullOrWhiteSpace(" ", "name");

        action.Should().Throw<ArgumentException>();
    }
}

public class ResultTests
{
    [Fact]
    public void SuccessResult_IsSuccessful()
    {
        var result = Result.Success();

        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
    }

    [Fact]
    public void FailureResult_ContainsError()
    {
        var error = Error.Validation("Invalid input");

        var result = Result.Failure(error);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(error);
    }
}
