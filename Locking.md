# `RequestCollapserPolicy` and locking

`ISyncLockProvider`/`IAsyncLockProvider` provide extension points to vary the locking mechanism used by `RequestCollapserPolicy`.

## How does `RequestCollapserPolicy` use locking internally?

`RequestCollapserPolicy` uses a lock internally to accurately identify concurrent duplicate calls.

The lock is _**not**_ held over slow calls to the underlying system; only for order-of-nanosecond-to-microsecond timings to atomically check or update the internal store of pending downstream calls.  

The underlying mechanism for collapsing duplicate requests is a `ConcurrentDictionary` by key, of `Lazy<T>`: a `ConcurrentDictionary<string, Lazy<T>>` for sync and `ConcurrentDictionary<string, Lazy<Task<T>>>` for async. 

## Doesn't `ConcurrentDictionary` include its own internal lock? Why does `RequestCollapserPolicy` add extra locking?

Direct methods on `ConcurrentDictionary<,>` are thread-safe and offer atomic methods for various operations, but there is no atomic overload for a particular operation required by the policy implementation: to atomically remove a given key from the dictionary iff it matches a given comparand.

Additionally, some users have a requirement for a distributed lock on a distributed key (held for example by Redis).  If one has for example a large server farm, or a large number of workers in kubernetes pods, it can be useful to lock on a distributed cache key so that 10s or 100s of worker instances don't all execute an expensive database query at the same time when a cache key expires, or perhaps on restart of a section of infrastructure.

## What is the v0.1.0 locking implementation? How does it perform?

The v0.1.0 implementation is a simple .NET `Monitor` scoped to the policy instance.  The lock is not held over slow calls to the underlying system; only for order-of-nanosecond-to-microsecond atomic operations on `ConcurrentDictionary<,>`.

Measured on a development machine in a [synthetic benchmark](https://github.com/reisenberger/LockContentionBenchMark_Issue657/) in which up to [50 parallel calls blocked while contending the lock](https://github.com/reisenberger/LockContentionBenchMark_Issue657/blob/master/ParallelContention50/ConcurrentDictionaryLockContention50.Benchmarks-report-github.md) ([notes](https://github.com/reisenberger/LockContentionBenchMark_Issue657/blob/master/BenchmarkNotes.md)), this lock implementation added an overall 20-30 microseconds (1/30000 to 1/50000 of a second) to the aggregate completion of 50 contending calls.  

This gives an indication of perf, but the usual ymmv ("your mileage may vary") caveats apply; you may experience different stats on your own servers.

## What other lock implementations are available?

Lock implementations which may be added include:

+ in-memory lock-per-key on a striped-locking basis, for high-contention use cases 
+ distributed locks held, eg, by Redis.