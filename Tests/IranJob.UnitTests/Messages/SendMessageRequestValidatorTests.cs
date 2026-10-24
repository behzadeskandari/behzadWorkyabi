using FluentAssertions;
using IranJob.Modules.Messages.Application.Contracts;
using IranJob.Modules.Messages.Application.Validators;

namespace IranJob.UnitTests.Messages;

public sealed class SendMessageRequestValidatorTests
{
    private readonly SendMessageRequestValidator _validator = new();

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\r\n")]
    public void Rejects_empty_or_whitespace_content(string content)
    {
        _validator.Validate(new SendMessageRequest(Guid.NewGuid(), content)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Rejects_content_over_domain_limit()
    {
        _validator.Validate(new SendMessageRequest(Guid.NewGuid(), new string('a', 20001))).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Accepts_trimmed_nonempty_content_within_limit()
    {
        _validator.Validate(new SendMessageRequest(Guid.NewGuid(), " hello ")).IsValid.Should().BeTrue();
    }
}
