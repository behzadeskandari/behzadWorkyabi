using System.IO;
using FluentValidation;
using IranJob.BuildingBlocks.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace IranJob.Modules.Candidates.Infrastructure.Services;

/// <summary>
/// Validates CV upload parameters (file name, content-type, size) against the
/// configured <see cref="FileStorageOptions"/>.
/// </summary>
public sealed class CvUploadValidator : AbstractValidator<CvUploadRequest>
{
    public CvUploadValidator(IOptions<FileStorageOptions> options)
    {
        var opts = options.Value;

        RuleFor(x => x.FileName)
            .NotEmpty().WithMessage("نام فایل الزامی است.")
            .Must((_, fileName) => BeAllowedExtension(fileName, opts.AllowedCvExtensions))
            .WithMessage("فقط پسوندهای زیر مجاز است: " + string.Join(", ", opts.AllowedCvExtensions));

        RuleFor(x => x.ContentType)
            .NotEmpty().WithMessage("نوع محتوا الزامی است.")
            .Must((_, contentType) => BeAllowedContentType(contentType, opts.AllowedCvContentTypes))
            .WithMessage("فقط انواع محتوای زیر مجاز است: " + string.Join(", ", opts.AllowedCvContentTypes));

        RuleFor(x => x.FileSize)
            .GreaterThan(0).WithMessage("فایل خالی است.")
            .LessThanOrEqualTo(opts.CvMaxFileSizeBytes).WithMessage(
                "حداکثر اندازه فایل CV " + (opts.CvMaxFileSizeBytes / 1024 / 1024) + " مگابایت است.");
    }

    private static bool BeAllowedExtension(string fileName, string[] allowedExtensions)
    {
        var ext = Path.GetExtension(fileName)?.ToLowerInvariant();
        return !string.IsNullOrEmpty(ext) && allowedExtensions.Contains(ext);
    }

    private static bool BeAllowedContentType(string contentType, string[] allowedContentTypes) =>
        allowedContentTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase);
}

public sealed record CvUploadRequest(string FileName, string ContentType, long FileSize);
