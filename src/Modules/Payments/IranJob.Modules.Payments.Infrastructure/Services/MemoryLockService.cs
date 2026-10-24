using System.Collections.Concurrent;
using System.Threading;

namespace IranJob.Modules.Payments.Infrastructure.Services;

/// <summary>
/// Simple in-memory lock service for protecting concurrent access to
/// settings updates within a single API instance. For multi-instance
/// deployments, back this with a distributed lock (e.g. Redis RedLock).
/// </summary>
public interface ILockService : IDisposable
{
    bool TryEnter(string key, TimeSpan timeout, out CancellationTokenSource cts);
}

public sealed class MemoryLockService : ILockService
{
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _lockMap = new(StringComparer.OrdinalIgnoreCase);
    private readonly SemaphoreSlim _globalGate = new(1, 1);

    public bool TryEnter(string key, TimeSpan timeout, out CancellationTokenSource cts)
    {
        if (!_globalGate.Wait(timeout))
        {
            cts = null!;
            return false;
        }

        try
        {
            if (_lockMap.TryGetValue(key, out var existing))
            {
                cts = existing;
                return false;
            }

            cts = new CancellationTokenSource();
            _lockMap[key] = cts;
            return true;
        }
        finally
        {
            _globalGate.Release();
        }
    }

    public void Dispose()
    {
        _globalGate.Dispose();
        foreach (var cts in _lockMap.Values)
            cts.Cancel();
        _lockMap.Clear();
    }
}
