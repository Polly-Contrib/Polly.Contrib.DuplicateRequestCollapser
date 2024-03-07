# Polly.Contrib.DuplicateRequestCollapser

The `Polly.Contrib.DuplicateRequestCollapser` nuget package provides `CacheStampedeResilienceStrategy`.

## What is `CacheStampedeResilienceStrategy`?

`CacheStampedeResilienceStrategy` is a [Polly](https://github.com/App-vNext/Polly/) policy which prevents duplicate requests executing concurrently.  

When duplicate requests (detected by matching key) are placed concurrently through the policy, the policy executes the underlying request delegate only _once_, and returns the same result value to all concurrent callers.

## Use Case: Avoiding cache repopulation storms

A primary use case is to prevent request storms when repopulating cached expensive data.  

Imagine a high throughput system which caches frequently-used data.  The query to create this data at the underlying system is expensive.

In a high-frequency system with 100s or 1000s of requests per second, when a popular cache key expires, it is possible that multiple external requests may arrive simultaneously which would invoke the underlying call to repopulate that cache key.  

If the underlying data is expensive/time-consuming to create, multiple parallel requests could potentially mount up, placing additional unnecessary stress on the downstream system.

### How does `CacheStampedeResilienceStrategy` solve this?

`CacheStampedeResilienceStrategy` solves this by collapsing concurrent duplicate executions into a single downstream call.  Requests are detected to be duplicates by matching key.

When a request arrives, the policy checks if it has a record of a pending downstream request for the same key.  If so, second and subsequent duplicate calls simply block on the same original request.  When the answer to that single request arrives, all blocking callers for that key are handed the answer. The cycle of concurrent-blocking for that key is then complete.

Requests are detected to be duplicates by matching key, making the policy is easy to combine with key-based caches.

Bear in mind, the collapser is not re-entrant safe. Do not call the collapser inside itself.

## How to use `CacheStampedeResilienceStrategy`?

    var pipeline = new ResiliencePipelineBuilder()
                       .AddCacheStampedeResilience(options)
                       .Build();

    // Now, convert it to a v7 policy. Note that it can be converted to both sync and async policies.
    ISyncPolicy syncPolicy = pipeline.AsSyncPolicy();
    IAsyncPolicy asyncPolicy = pipeline.AsAsyncPolicy();

    // Finally, use it in a policy wrap.
    ISyncPolicy wrappedPolicy = Policy.Wrap(
        syncPolicy,
        cachePolicy);

    var result = await syncPolicy.WrapAsync(cachePolicy)
        .ExecuteAsync(context => GetExpensiveFoo(), new Context("SomeKey"));

_**Note:**_ A `ResilienceContext` must be passed to the execution which, when combined with the key strategy (see below) specifies a key.  If no key is specified, the policy is a no-op (no request collapsing occurs).

The example above demonstrates a correct pattern when using the default key strategy.  

## How to configure `CacheStampedeResilienceStrategy`?

    var options = new CacheStampedeResilienceStrategyOptions();
                options.KeyStrategy = keyStrategy// optional
                options.LockProvider = lockProvider // optional
                
    var pipeline = new ResiliencePipelineBuilder()
                       .AddCacheStampedeResilience(options)
                       .Build()

Similar configuration overloads exist for strongly-typed forms `CacheStampedeResilienceStrategy<TResult>`.

### `IKeyStrategy` parameter

An optional `IKeyStrategy` can be supplied to control how the collapser key is obtained or generated from the Polly execution `ResilienceContext`.  The `IKeyStrategy` interface defines a single method `string? GetKey(Context context)`.

#### Default if `IKeyStrategy` is not supplied

The default when `IKeyStrategy` is not supplied takes `ResilienceContext.OperationKey` as the key to identify duplicates.  This is the [same default key as for Polly's CachePolicy](https://github.com/App-vNext/Polly/wiki/Cache#default-cachekeystrategy-if-none-specified).

### `ILockProvider` property

`ILockProvider` provides an optional mechanism to vary the locking mechanism used by `CacheStampedeResilienceStrategy` from the default.

#### How does `CacheStampedeResilienceStrategy` use locking?

`CacheStampedeResilienceStrategy` uses a lock internally to accurately identify concurrent duplicate calls.

_**Note**_: The lock is _**not**_ held over slow calls to the underlying system; only for order-of-nanosecond-to-microsecond timings to atomically check or update the internal store of pending downstream calls.  

See [separate page on locking](Locking.md) for more details.

## How to combine `CacheStampedeResilienceStrategy` with `CachePolicy`?

Use [`PolicyWrap`](https://github.com/App-vNext/Polly/wiki/PolicyWrap) to combine policies.  For more on the Polly `CachePolicy`, see the [Polly Cache wiki](https://github.com/App-vNext/Polly/wiki/Cache).

### Recommended for distributed caches: `collapser -> cache -> underlying call`

    var pipeline = new ResiliencePipelineBuilder()
                       .AddCacheStampedeResilience(options)
                       .Build();

    // Now, convert it to a v7 policy. Note that it can be converted to both sync and async policies.
    ISyncPolicy syncPolicy = pipeline.AsSyncPolicy();
    IAsyncPolicy asyncPolicy = pipeline.AsAsyncPolicy();

    // Finally, use it in a policy wrap.
    ISyncPolicy wrappedPolicy = Policy.Wrap(
        syncPolicy,
        cachePolicy);

    var result = wrappedPolicy
        .Execute(context => GetExpensiveFoo(), new Context("SomeKey"));

is the recommended pattern with distributed caches.  On cache expiry, both making the underlying expensive call and storing in the distributed cache will occur only once.

### Alternative for memory caches: `cache -> collapser -> underlying call`

    var pipeline = new ResiliencePipelineBuilder()
                       .AddCacheStampedeResilience(options)
                       .Build();

    // Now, convert it to a v7 policy. Note that it can be converted to both sync and async policies.
    ISyncPolicy syncPolicy = pipeline.AsSyncPolicy();
    IAsyncPolicy asyncPolicy = pipeline.AsAsyncPolicy();

    // Finally, use it in a policy wrap.
    ISyncPolicy wrappedPolicy = Policy.Wrap(
        cachePolicy
        syncPolicy);

    var result = wrappedPolicy
        .Execute(context => GetExpensiveFoo(), new Context("SomeKey"));

is a viable alternative for memory caches.  

On cache expiry:

+ The collapser policy guarantees the expensive underlying call will only execute once.
+ Multiple calls collapsed by the collapser policy will all return quasi-simultaneously, and the result will be placed in the memory cache multiple times.  As this is a memory cache, this is not as expensive.

### Double-caching: memory-cache and distributed-cache

If using both a memory cache and distributed cache:

    var doubleCacheWithCollapser = Policy.WrapAsync(memoryCachePolicy, collapserPolicy, distributedCachePolicy);
    var result = await doubleCacheWithCollapser.ExecuteAsync(context => GetExpensiveFoo(), new Context("SomeKey"));

When double-caching, take care to consider how both cache policies' TTLs interact in the overall call.  


## How to scope `CacheStampedeResilienceStrategy` instances?

`CacheStampedeResilienceStrategy` is stateful: it maintains records of current downstream pending calls. 

For the policy to function, you must use the same single stateful instance across calls and call sites for which you wish to collapse duplicate concurrent requests. Do not create a new policy instance per call or call site.

## What forms is `CacheStampedeResilienceStrategy` available in?

`CacheStampedeResilienceStrategy` is available in the usual four forms for Polly policies:

| Policy | use for |
| -- | -- |
| `CacheStampedeResilienceStrategy` | asynchronous `Func<ValueTask>` calls; or functions returning`Func<ValueTask<TResult>>` |
| `CacheStampedeResilienceStrategy<TResult>` | strongly-typed for synchronous calls returning `Func<ValueTask<TResult>>` |


## What about collapsing duplicate requests which are not concurrent?

A mechanism for collapsing duplicate requests which are _not_ concurrent is otherwise known as a cache ;~). Use [`CachePolicy`](https://github.com/App-vNext/Polly/wiki/Cache).

## What contexts can `CacheStampedeResilienceStrategy` be used in?

The code examples here demonstrate typical usage in conjunction with a cache policy.  However, `CacheStampedeResilienceStrategy` could be used in any ResilienceContext.  For example, you may have expensive initialization on startup of an app - perhaps to configure some expensive in-memory singleton - which must be performed only once however many incoming requests arrive. `CacheStampedeResilienceStrategy` could be used in this context too.

## What targets are supported?

`CacheStampedeResilienceStrategy` supports .Net Standard 2.0 upwards.  The package also offers direct tfms for .NET 4.6.1 and .NET 4.7.2

## Credits

Thanks to [@mrmartan](https://github.com/mrmartan) for the original idea, and to [@jjxtra](https://github.com/jjxtra) and [@phatcher](https://github.com/phatcher) for deep contributions to thinking.  The v0.1.0 implementation is by [@reisenberger](https://github.com/reisenberger) of the Polly team. 