using System.Threading.RateLimiting;
using Asp.Versioning;
using IranJob.Api.Services;
using IranJob.BuildingBlocks.Infrastructure.Extensions;
using IranJob.BuildingBlocks.Infrastructure.Logging;
using IranJob.Modules.Candidates.Infrastructure.Extensions;
using IranJob.Modules.Candidates.Presentation.Controllers;
using IranJob.Modules.EmployerProfile.Infrastructure.Extensions;
using IranJob.Modules.EmployerProfile.Presentation.Controllers;
using IranJob.Modules.Identity.Infrastructure.Configuration;
using IranJob.Modules.Identity.Infrastructure.Extensions;
using IranJob.Modules.Identity.Presentation.Controllers;
using IranJob.Modules.JobPostings.Infrastructure.Extensions;
using IranJob.Modules.JobPostings.Presentation.Controllers;
using IranJob.Modules.ReferenceData.Infrastructure.Extensions;
using IranJob.Modules.ReferenceData.Presentation.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.FileProviders;
using Microsoft.OpenApi.Models;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithEnvironmentName()
        .Enrich.WithThreadId()
        .Enrich.WithProperty("Application", context.Configuration["Application:Name"] ?? "IranJob")
        .Destructure.With<SensitiveDataDestructuringPolicy>()
        .WriteTo.Console(
            outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}");
});

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddIdentityModule(builder.Configuration);
builder.Services.AddCandidatesModule(builder.Configuration);
builder.Services.AddEmployerProfileModule(builder.Configuration);
builder.Services.AddReferenceDataModule(builder.Configuration);
builder.Services.AddJobPostingsModule(builder.Configuration);

var rateOptions = builder.Configuration.GetSection(IranJob.Modules.Identity.Infrastructure.Configuration.RateLimitingOptions.SectionName).Get<IranJob.Modules.Identity.Infrastructure.Configuration.RateLimitingOptions>()
    ?? new IranJob.Modules.Identity.Infrastructure.Configuration.RateLimitingOptions();

builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy(IranJob.Modules.Identity.Infrastructure.Extensions.ServiceCollectionExtensions.AuthRateLimitPolicy, httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = rateOptions.PermitLimit,
                Window = TimeSpan.FromSeconds(rateOptions.WindowSeconds),
                QueueLimit = 0
            }));

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

builder.Services.AddScoped<ISystemInfoService, SystemInfoService>();

builder.Services.AddControllers()
    .AddApplicationPart(typeof(AuthController).Assembly)
    .AddApplicationPart(typeof(CandidateProfileController).Assembly)
    .AddApplicationPart(typeof(EmployerProfileController).Assembly)
    .AddApplicationPart(typeof(JobCategoriesController).Assembly)
    .AddApplicationPart(typeof(EmployerJobsController).Assembly);

builder.Services.AddEndpointsApiExplorer();

builder.Services
    .AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
    })
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "IranJob API",
        Version = "v1",
        Description = "IranJob recruitment platform API"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
            .WithExposedHeaders("X-CSRF-TOKEN", "X-Correlation-ID");
    });
});

var app = builder.Build();

await app.ApplyInfrastructureMigrationsAsync();
await app.ApplyIdentityMigrationsAsync();
await app.ApplyCandidatesMigrationsAsync();
await app.ApplyEmployerProfileMigrationsAsync();
await app.ApplyReferenceDataMigrationsAsync();
await app.ApplyJobPostingsMigrationsAsync();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "IranJob API v1");
    });
}

app.UseExceptionHandler();
app.UseCors("Frontend");
app.UseInfrastructureMiddleware();
app.UseIdentityModule();

app.MapControllers();

app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("live")
});

app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready")
});


var uploadsPath = Path.Combine(app.Environment.ContentRootPath, "uploads");

app.UseStaticFiles(); // normal wwwroot (optional but recommended)

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads"          // ← this makes /uploads/logos/... work
});

app.Run();

public partial class Program;
