using Polly.Contrib.DuplicateRequestCollapser.Utils;
using Polly.Telemetry;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Polly.Contrib.DuplicateRequestCollapser;

/// <summary>
/// A concurrent duplicate request collapser policy. Code executed through the policy is executed as if no policy was applied.
/// </summary>
internal sealed class CacheStampedeResilienceStrategy : ResilienceStrategy
{
    private readonly ResilienceStrategyTelemetry _telemetry;
    private readonly IKeyStrategy _keyStrategy;
    private readonly ILockProvider _lockProvider;
    private readonly ConcurrentDictionary<string, Lazy<ValueTask<object>>> _collapser = new ConcurrentDictionary<string, Lazy<ValueTask<object>>>();

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="options">Strategy Options</param>
    /// <param name="telemetry">Telemetry</param>
    public CacheStampedeResilienceStrategy(CacheStampedeResilienceStrategyOptions options, ResilienceStrategyTelemetry telemetry)
    {
        Guard.NotNull(telemetry, nameof(telemetry));
        Guard.NotNull(options.KeyStrategy, nameof(options.KeyStrategy));
        Guard.NotNull(options.LockProvider, nameof(options.LockProvider));
        _keyStrategy = options.KeyStrategy;
        _lockProvider = options.LockProvider;
        _telemetry = telemetry;
    }

    /// <inheritdoc/>
    protected override async ValueTask<Outcome<TResult>> ExecuteCore<TResult, TState>(Func<ResilienceContext, TState, ValueTask<Outcome<TResult>>> callback, ResilienceContext context, TState state)
    {
        context.CancellationToken.ThrowIfCancellationRequested();

        string? key = _keyStrategy.GetKey(context);

        // Fast-path if no key specified on Context (similar to CachePolicy).
        if (key == null)
        {
            var outcome = await StrategyHelper.ExecuteCallbackSafeAsync(callback, context, state).ConfigureAwait(context.ContinueOnCapturedContext);
            _telemetry.Report(new ResilienceEvent(ResilienceEventSeverity.Information, CacheStampedeResilienceOutcomeConstants.NoKey), context, new NoKeyArguments());
            return outcome;
        }
        Lazy<ValueTask<object>> lazy;

        await using (_lockProvider.AcquireLockAsync(key, context, context.CancellationToken, context.ContinueOnCapturedContext).ConfigureAwait(context.ContinueOnCapturedContext))
        {
            lazy = _collapser.GetOrAdd(key, new Lazy<ValueTask<object>>(async () => await StrategyHelper.ExecuteCallbackSafeAsync(callback, context, state).ConfigureAwait(context.ContinueOnCapturedContext), LazyThreadSafetyMode.ExecutionAndPublication)); // Note: per documentation, LazyThreadSafetyMode.ExecutionAndPublication guarantees single execution, but means the executed code must not lock, as this risks deadlocks.  We should document.
        }

        try
        {
            var callbackTask = lazy.Value;
            if (callbackTask.IsCompleted)
            {
                _telemetry.Report(new ResilienceEvent(ResilienceEventSeverity.Information, CacheStampedeResilienceOutcomeConstants.IsCompleted), context, new IsCompletedArguments());
                return /*Outcome.FromResult(*/(Outcome<TResult>)callbackTask.GetResult()/*)*/;
            }
            var outcome = await callbackTask.ConfigureAwait(context.ContinueOnCapturedContext);
            _telemetry.Report(new ResilienceEvent(ResilienceEventSeverity.Information, CacheStampedeResilienceOutcomeConstants.IsCompleted), context, new IsCompletedArguments());

            return /*Outcome.FromResult(*/(Outcome<TResult>)outcome/*)*/;
        }
        catch (Exception e)
        {
            _telemetry.Report(new(ResilienceEventSeverity.Error, CacheStampedeResilienceOutcomeConstants.OnException), context, new OnExceptionArguments());
            return Outcome.FromException<TResult>(e);
        }
        finally
        {
            // As soon as the lazy has returned a result to one thread, the concurrent request set is over, so we evict the lazy from the ConcurrentDictionary.
            // We need to evict within a lock, to be sure we are not, due to potential race with new threads populating, evicting a different lazy created by a different thread.
            // To reduce lock contention, first check outside the lock whether we still need to remove it (we will double-check inside the lock).
            Lazy<ValueTask<object>>? currentValue = null;
            if (_collapser.TryGetValue(key, out currentValue))
            {
                if (currentValue == lazy)
                {
                    await using (_lockProvider.AcquireLockAsync(key, context, context.CancellationToken, context.ContinueOnCapturedContext)
                        .ConfigureAwait(context.ContinueOnCapturedContext))
                    {
                        Lazy<ValueTask<object>>? valueWithinLock = null;
                        // Double-check that there has not been a race which updated the dictionary with a new value.
                        if (_collapser.TryGetValue(key, out valueWithinLock))
                        {
                            if (valueWithinLock == lazy)
                            {
                                _collapser.TryRemove(key, out _);
                            }
                        }
                    }
                }
            }
        }
    }
}
