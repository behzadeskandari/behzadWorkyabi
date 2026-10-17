using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using IranJob.Modules.JobPostings.Application.Contracts;
using IranJob.Modules.JobPostings.Infrastructure.Persistence;
using IranJob.Modules.Payments.Application.Abstractions;
using IranJob.Modules.Payments.Application.Contracts;
using IranJob.Modules.Payments.Domain.Enums;
using IranJob.Modules.Payments.Infrastructure.Persistence;
using IranJob.Modules.ReferenceData.Domain.Entities;
using IranJob.Modules.ReferenceData.Infrastructure.Persistence;
using IranJob.BuildingBlocks.Infrastructure.Events;
using IranJob.IntegrationTests.TestStubs;
using IranJob.Modules.Payments.Domain.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

// Serialise payment tests — the StubZarinPalClient is a singleton and cannot
// tolerate parallel writes to its VerifyResult / RequestResult properties.
namespace IranJob.IntegrationTests;

[CollectionDefinition("PaymentTests", DisableParallelization = true)]
public class PaymentTestsCollection;

/// <summary>
/// Integration tests for the payment module covering initiation, callback verification,
/// job-publish gating, idempotency, concurrency, cross-user protection, and sensitive
/// data handling. All gateway calls go through <see cref="StubZarinPalClient"/>.
/// </summary>
[Collection("PaymentTests")]
public sealed class PaymentTests : IClassFixture<CustomWebApplicationFactory>
{
    private const string JobsEndpoint = "/api/v1/employers/jobs";
    private const string EmployerProfileEndpoint = "/api/v1/employers/profile";
    private const string PaymentsInitiateEndpoint = "/api/v1/employers/payments/initiate";
    private const string PaymentsVerifyEndpoint = "/api/v1/employers/payments/verify";
    private const long PublishFee = 10000;

    private readonly CustomWebApplicationFactory factory;
    private readonly JsonSerializerOptions json = new(JsonSerializerDefaults.Web);
    private static int userCounter;

    public PaymentTests(CustomWebApplicationFactory factory)
    {
        this.factory = factory;
        factory.PublishFeeAmount = PublishFee;
    }

    [Fact]
    public async Task Payment_Initiation_CreatesPersistedPayment()
    {
        var (client, profileId, jobId) = await CreateEmployerAndDraftJobAsync();

        var publishResponse = await client.PostAsync($"{JobsEndpoint}/{jobId}/publish", null);
        publishResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await publishResponse.Content.ReadFromJsonAsync<JsonElement>(json);
        body.GetProperty("status").GetString().Should().Be("Draft");
        body.TryGetProperty("paymentInfo", out var paymentInfo).Should().BeTrue();
        paymentInfo.GetProperty("paymentId").GetGuid().Should().NotBeEmpty();
        paymentInfo.GetProperty("amount").GetInt64().Should().Be(PublishFee);
        paymentInfo.GetProperty("currency").GetString().Should().Be("IRR");
        paymentInfo.GetProperty("status").GetString().Should().Be("Pending");
        paymentInfo.GetProperty("completed").GetBoolean().Should().BeFalse();
        var paymentId = paymentInfo.GetProperty("paymentId").GetGuid();

        using var scope = factory.Services.CreateScope();
        var paymentDb = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
        var payment = await paymentDb.Payments.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == paymentId);
        payment.Should().NotBeNull();
        payment!.Amount.Should().Be(PublishFee);
        payment.Currency.Should().Be(Currency.IRR);
        payment.Status.Should().Be(PaymentStatus.Pending);
        payment.EmployerProfileId.Should().Be(profileId);
        payment.RelatedJobPostingId.Should().Be(jobId);
        payment.Authority.Should().NotBeNullOrEmpty();
        payment.RowVersion.Should().NotBeNull().And.HaveCount(8);
    }

    [Fact]
    public async Task FailedGatewayVerification_NeverMarksPaymentSuccessful()
    {
        var (client, _, _, paymentId, authority) = await CreateEmployerJobAndInitiatePaymentAsync(
            verifyResult: new ZarinPalVerifyResult(-1, "FAILED", null));

        var verifyResponse = await client.GetAsync(
            $"{PaymentsVerifyEndpoint}?authority={authority}&status=1");
        verifyResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await verifyResponse.Content.ReadFromJsonAsync<JsonElement>(json);
        body.GetProperty("status").GetString().Should().Be("Failed");

        using var scope = factory.Services.CreateScope();
        var paymentDb = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
        var payment = await paymentDb.Payments.AsNoTracking().FirstAsync(p => p.Id == paymentId);
        payment.Status.Should().Be(PaymentStatus.Failed);
        payment.FailedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task SuccessfulGatewayVerification_ValidatesAmountAndIdentity()
    {
        var (client, _, _, _, authority) = await CreateEmployerJobAndInitiatePaymentAsync();
        var paymentId = await GetPaymentIdByAuthorityAsync(authority);

        var verifyResponse = await client.GetAsync(
            $"{PaymentsVerifyEndpoint}?authority={authority}&status=1");
        verifyResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var stub = GetStub();
        stub.LastVerifyRequest.Should().NotBeNull();
        stub.LastVerifyRequest!.Amount.Should().Be(PublishFee);
        stub.LastVerifyRequest.Authority.Should().Be(authority);

        using var scope = factory.Services.CreateScope();
        var paymentDb = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
        var payment = await paymentDb.Payments.AsNoTracking().FirstAsync(p => p.Id == paymentId);
        payment.Status.Should().Be(PaymentStatus.Paid);
        payment.PaidAt.Should().NotBeNull();
    }

    [Fact]
    public async Task UnpaidJob_StaysDraft_WhenPaymentIsRequired()
    {
        GetStub().Reset();
        var (client, _, jobId) = await CreateEmployerAndDraftJobAsync();

        var publishResponse = await client.PostAsync($"{JobsEndpoint}/{jobId}/publish", null);
        publishResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await publishResponse.Content.ReadFromJsonAsync<JsonElement>(json);
        body.GetProperty("status").GetString().Should().Be("Draft");
        body.TryGetProperty("paymentInfo", out _).Should().BeTrue();
    }

    [Fact]
    public async Task VerifiedPayment_PublishesJob()
    {
        var (client, _, _, _, authority) = await CreateEmployerJobAndInitiatePaymentAsync();
        var paymentId = await GetPaymentIdByAuthorityAsync(authority);
        var jobId = await GetJobIdByPaymentAsync(paymentId);

        using (var scope = factory.Services.CreateScope())
        {
            var jobDb = scope.ServiceProvider.GetRequiredService<JobPostingDbContext>();
            var job = await jobDb.JobPostings.AsNoTracking().FirstAsync(j => j.Id == jobId);
            job.Status.ToString().Should().Be("Draft");
        }

        var verifyResponse = await client.GetAsync(
            $"{PaymentsVerifyEndpoint}?authority={authority}&status=1");
        verifyResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        await Task.Delay(200);
        using (var scope = factory.Services.CreateScope())
        {
            var jobDb = scope.ServiceProvider.GetRequiredService<JobPostingDbContext>();
            var job = await jobDb.JobPostings.AsNoTracking().FirstAsync(j => j.Id == jobId);
            job.Status.ToString().Should().Be("Published");
            job.PublishedAt.Should().NotBeNull();
        }
    }

    [Fact]
    public async Task RepeatedCallbacks_DoNotDuplicateProcessing()
    {
        var stub = GetStub();
        stub.Reset();

        var (client, _, _, _, authority) = await CreateEmployerJobAndInitiatePaymentAsync();

        var firstResponse = await client.GetAsync(
            $"{PaymentsVerifyEndpoint}?authority={authority}&status=1");
        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var firstBody = await firstResponse.Content.ReadFromJsonAsync<JsonElement>(json);
        firstBody.GetProperty("status").GetString().Should().Be("Paid");

        var secondResponse = await client.GetAsync(
            $"{PaymentsVerifyEndpoint}?authority={authority}&status=1");
        secondResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var secondBody = await secondResponse.Content.ReadFromJsonAsync<JsonElement>(json);
        secondBody.GetProperty("status").GetString().Should().Be("Paid");
        secondBody.GetProperty("message").GetString().Should().Be("Already verified");

        stub.VerifyCallCount.Should().Be(1);
    }

            [Fact]
    public async Task ConcurrentVerifications_DoNotCauseInconsistentState()
    {
        // Each verification runs in its own request-scoped DbContext; with the masking
        // broad-retry removed, concurrent callbacks must both return 200 (no
        // NullReferenceException / SQLITE_BUSY) and converge on a single consistent state.
        var (client, _, _, paymentId, authority) =
            await CreateEmployerJobAndInitiatePaymentAsync();
        var jobId = await GetJobIdByPaymentAsync(paymentId);

        var tasks = Enumerable.Range(1, 2)
            .Select(_ => client.GetAsync(
                $"{PaymentsVerifyEndpoint}?authority={authority}&status=1"))
            .ToArray();

        var responses = await Task.WhenAll(tasks);
        responses.Should().HaveCount(2);
        responses.Should().OnlyContain(r => r.StatusCode == HttpStatusCode.OK,
            "shared-cache + per-DbContext connections isolate concurrent verifies; no 409/500");

        using var scope = factory.Services.CreateScope();
        var paymentDb = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
        var payment = await paymentDb.Payments.AsNoTracking().FirstAsync(p => p.Id == paymentId);
        payment.Status.Should().Be(PaymentStatus.Paid);
        payment.PaidAt.Should().NotBeNull();

        var jobDb = scope.ServiceProvider.GetRequiredService<JobPostingDbContext>();
        var job = await jobDb.JobPostings.AsNoTracking().FirstAsync(j => j.Id == jobId);
        job.Status.ToString().Should().Be("Published");
        job.PublishedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task CrossUserPaymentVerification_ReturnsForbidden()
    {
        var (clientA, _, _, _, authority) = await CreateEmployerJobAndInitiatePaymentAsync();

        var clientB = await CreateAuthenticatedClientAsync("Employer");
        await CreateEmployerProfileAsync(clientB);

        var response = await clientB.GetAsync(
            $"{PaymentsVerifyEndpoint}?authority={authority}&status=1");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GatewayRejectsPaymentRequest_MarksInitialPaymentFailed()
    {
        var stub = GetStub();
        stub.Reset();
        stub.RequestResult = new ZarinPalRequestResult(-1, "GATEWAY_ERROR", string.Empty);

        var (client, _, jobId) = await CreateEmployerAndDraftJobAsync();

        var publishResponse = await client.PostAsync($"{JobsEndpoint}/{jobId}/publish", null);
        publishResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await publishResponse.Content.ReadFromJsonAsync<JsonElement>(json);
        body.GetProperty("status").GetString().Should().Be("Draft");
        body.TryGetProperty("paymentInfo", out var paymentInfo).Should().BeTrue();
        paymentInfo.GetProperty("status").GetString().Should().Be("Failed");

        using var scope = factory.Services.CreateScope();
        var paymentDb = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
        var payment = await paymentDb.Payments.AsNoTracking()
            .FirstOrDefaultAsync(p => p.RelatedJobPostingId == jobId);
        payment.Should().NotBeNull();
        payment!.Status.Should().Be(PaymentStatus.Failed);
    }

    [Fact]
    public async Task CanceledPayment_ResultsInFailedStatus()
    {
        var (client, _, _, paymentId, authority) =
            await CreateEmployerJobAndInitiatePaymentAsync();

        var response = await client.GetAsync(
            $"{PaymentsVerifyEndpoint}?authority={authority}&status=cancel");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(json);
        body.GetProperty("status").GetString().Should().Be("Failed");

        using var scope = factory.Services.CreateScope();
        var paymentDb = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
        var payment = await paymentDb.Payments.AsNoTracking().FirstAsync(p => p.Id == paymentId);
        payment.Status.Should().Be(PaymentStatus.Failed);
    }

    [Fact]
    public async Task ApiResponse_DoesNotExposeSensitiveData()
    {
        var (client, _, _, _, authority) = await CreateEmployerJobAndInitiatePaymentAsync();

        var verifyResponse = await client.GetAsync(
            $"{PaymentsVerifyEndpoint}?authority={authority}&status=1");
        verifyResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var rawBody = await verifyResponse.Content.ReadAsStringAsync();
        var lowerBody = rawBody.ToLowerInvariant();
        lowerBody.Should().NotContain("merchant_id");
        lowerBody.Should().NotContain("merchantid");
        lowerBody.Should().NotContain("secret");
        lowerBody.Should().NotContain("apikey");

        var createPaymentRequest = new CreatePaymentRequest(null, 5000, "IRR", "Test", null);
        var initiateResponse = await client.PostAsJsonAsync(
            PaymentsInitiateEndpoint, createPaymentRequest, json);
        initiateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var initiateBody = await initiateResponse.Content.ReadAsStringAsync();
        var lowerInitiate = initiateBody.ToLowerInvariant();
        lowerInitiate.Should().NotContain("merchant_id");
        lowerInitiate.Should().NotContain("merchantid");
        lowerInitiate.Should().NotContain("secret");
        lowerInitiate.Should().NotContain("apikey");
    }

    private StubZarinPalClient GetStub() =>
        factory.Services.GetRequiredService<IZarinPalClient>() as StubZarinPalClient
        ?? throw new InvalidOperationException("StubZarinPalClient is not registered.");

    private async Task<Guid> GetPaymentIdByAuthorityAsync(string authority)
    {
        using var scope = factory.Services.CreateScope();
        var paymentDb = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
        var payment = await paymentDb.Payments.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Authority == authority);
        payment.Should().NotBeNull();
        return payment!.Id;
    }

    private async Task<Guid> GetJobIdByPaymentAsync(Guid paymentId)
    {
        using var scope = factory.Services.CreateScope();
        var paymentDb = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
        var payment = await paymentDb.Payments.AsNoTracking()
            .FirstAsync(p => p.Id == paymentId);
        payment.RelatedJobPostingId.Should().NotBeNull();
        return payment.RelatedJobPostingId!.Value;
    }

    private async Task<(HttpClient client, Guid profileId)> CreateEmployerProfileAsync(HttpClient? client = null)
    {
        var c = client ?? await CreateAuthenticatedClientAsync("Employer");

        var profileResponse = await c.PostAsJsonAsync(EmployerProfileEndpoint, new
        {
            companyName = $"Company {Guid.NewGuid():N}",
            industry = "Technology",
            companySize = "11-50",
            city = "Tehran",
            province = "Tehran"
        }, json);

        profileResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await profileResponse.Content.ReadFromJsonAsync<JsonElement>(json);
        var profileId = body.GetProperty("id").GetGuid();
        return (c, profileId);
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync(string role)
    {
        await factory.InitializeDatabaseAsync();
        var number = Interlocked.Increment(ref userCounter);
        var email = $"payment-{role}-{number}@test.ir";
        var client = factory.CreateClient();
        var registration = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            firstName = "Test",
            lastName = "Payment",
            email,
            phoneNumber = $"0988{number:D7}",
            password = "Passw0rd!",
            role
        }, json);
        registration.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var login = await client.PostAsJsonAsync("/api/v1/auth/login",
            new { identifier = email, password = "Passw0rd!" }, json);
        login.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await login.Content.ReadFromJsonAsync<JsonElement>(json);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", body.GetProperty("accessToken").GetString());
        return client;
    }

    private async Task<ReferenceIds> SeedReferenceDataAsync()
    {
        await factory.InitializeDatabaseAsync();
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ReferenceDataDbContext>();

        var categoryName = $"Software {Guid.NewGuid():N}";
        var skillName = $"Skill {Guid.NewGuid():N}";
        var country = Country.Create("Test Country", UniqueCountryCode());
        dbContext.Countries.Add(country);
        var province = Province.Create(country.Id, $"Test Province {Guid.NewGuid():N}");
        dbContext.Provinces.Add(province);
        var city = City.Create(province.Id, $"Test City {Guid.NewGuid():N}");
        dbContext.Cities.Add(city);
        var category = JobCategory.Create(categoryName, null);
        dbContext.JobCategories.Add(category);
        var skill = Skill.Create(skillName, null);
        dbContext.Skills.Add(skill);
        await dbContext.SaveChangesAsync();
        return new ReferenceIds(category.Id, category.Name, skill.Id, skill.Name, city.Id, city.Name);
    }

    private static string UniqueCountryCode() => new string(Guid.NewGuid().ToByteArray()
        .Take(3).Select(value => (char)('A' + value % 26)).ToArray());

    private async Task<Guid> CreateDraftJobAsync(HttpClient client, ReferenceIds references)
    {
        var request = new CreateJobPostingRequest
        {
            Title = $"Test Job {Guid.NewGuid():N}",
            Description = "A test job posting description that is sufficiently detailed.",
            CategoryId = references.CategoryId,
            SkillIds = [references.SkillId],
            EmploymentType = "FullTime",
            WorkArrangement = "OnSite",
            CityId = references.CityId,
            SalaryMinimum = 50000m,
            SalaryMaximum = 80000m,
            SalaryCurrency = "IRR",
            SalaryPeriod = "Monthly",
            ClosingAt = DateTimeOffset.UtcNow.AddDays(30)
        };

        var response = await client.PostAsJsonAsync(JobsEndpoint, request, json);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(json);
        return body.GetProperty("id").GetGuid();
    }

    private async Task<(HttpClient client, Guid profileId, Guid jobId)> CreateEmployerAndDraftJobAsync()
    {
        var references = await SeedReferenceDataAsync();
        var (client, profileId) = await CreateEmployerProfileAsync();
        var jobId = await CreateDraftJobAsync(client, references);
        return (client, profileId, jobId);
    }

    private async Task<(HttpClient client, Guid profileId, Guid jobId, Guid paymentId, string authority)>
        CreateEmployerJobAndInitiatePaymentAsync(
            ZarinPalVerifyResult? verifyResult = null,
            ZarinPalRequestResult? requestResult = null)
    {
        var stub = GetStub();
        stub.Reset();
        if (verifyResult != null) stub.VerifyResult = verifyResult;
        if (requestResult != null) stub.RequestResult = requestResult;

        var (client, profileId, jobId) = await CreateEmployerAndDraftJobAsync();

        var publishResponse = await client.PostAsync($"{JobsEndpoint}/{jobId}/publish", null);
        publishResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = factory.Services.CreateScope();
        var paymentDb = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
        var payment = await paymentDb.Payments.AsNoTracking()
            .FirstOrDefaultAsync(p => p.RelatedJobPostingId == jobId);
        payment.Should().NotBeNull();
        payment!.Authority.Should().NotBeNullOrEmpty();

        return (client, profileId, jobId, payment.Id, payment!.Authority!);
    }

    private sealed record ReferenceIds(Guid CategoryId, string CategoryName,
        Guid SkillId, string SkillName, Guid CityId, string CityName);
}

