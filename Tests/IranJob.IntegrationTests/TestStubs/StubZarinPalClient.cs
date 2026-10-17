using System.Threading;
using IranJob.Modules.Payments.Application.Abstractions;
using IranJob.Modules.Payments.Application.Contracts;

namespace IranJob.IntegrationTests.TestStubs;

/// <summary>
/// Deterministic stand-in for <see cref="IZarinPalClient"/> used by the integration
/// test host. Simulates a successful ZarinPal v4 response (code 100 / authority
/// "test-authority") so job-publish fee gating can be exercised without real HTTP.
///
/// <para>Set <see cref="RequestResult"/> or <see cref="VerifyResult"/> to <c>null</c>
/// (the default) to get the default success response. Set them to a custom result
/// (e.g. a failure code) to simulate gateway errors. Each call to <see cref="Reset"/>
/// clears the configurable overrides and counters.</para>
/// </summary>
public sealed class StubZarinPalClient : IZarinPalClient
{
    private ZarinPalRequestResult? _requestResult;
    private ZarinPalVerifyResult? _verifyResult;
    private int _verifyCallCount;
    private int _authoritySequence;
    private ZarinPalRequest? _lastRequest;
    private ZarinPalVerifyRequest? _lastVerifyRequest;

    /// <summary>
    /// When set, returned as-is from <see cref="RequestAsync"/>. When <c>null</c>,
    /// a default success result with a unique authority is generated.
    /// </summary>
    public ZarinPalRequestResult? RequestResult { get => _requestResult; set => _requestResult = value; }

    /// <summary>
    /// When set, returned as-is from <see cref="VerifyAsync"/>. When <c>null</c>,
    /// a default success result (code 100) is generated.
    /// </summary>
    public ZarinPalVerifyResult? VerifyResult { get => _verifyResult; set => _verifyResult = value; }

    /// <summary>Number of times <see cref="VerifyAsync"/> was called.</summary>
    public int VerifyCallCount => _verifyCallCount;

    /// <summary>The last request received by <see cref="RequestAsync"/>.</summary>
    public ZarinPalRequest? LastRequest => _lastRequest;

    /// <summary>The last request received by <see cref="VerifyAsync"/>.</summary>
    public ZarinPalVerifyRequest? LastVerifyRequest => _lastVerifyRequest;

    /// <summary>Clears configurable overrides and resets counters (not the authority sequence).</summary>
    public void Reset()
    {
        _requestResult = null;
        _verifyResult = null;
        _verifyCallCount = 0;
        _lastRequest = null;
        _lastVerifyRequest = null;
    }

    public Task<ZarinPalRequestResult> RequestAsync(ZarinPalRequest request, CancellationToken cancellationToken)
    {
        _lastRequest = request;
        if (_requestResult is { } result)
            return Task.FromResult(result);

        var authority = $"test-authority-{Interlocked.Increment(ref _authoritySequence)}";
        return Task.FromResult(new ZarinPalRequestResult(100, "SUCCESS", authority));
    }

    public Task<ZarinPalVerifyResult> VerifyAsync(ZarinPalVerifyRequest request, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _verifyCallCount);
        _lastVerifyRequest = request;
        if (_verifyResult is { } result)
            return Task.FromResult(result);

        return Task.FromResult(new ZarinPalVerifyResult(100, "SUCCESS", "ref-001"));
    }
}
