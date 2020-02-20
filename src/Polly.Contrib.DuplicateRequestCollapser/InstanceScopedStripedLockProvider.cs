#nullable enable

using System;
using System.Threading;

namespace Polly.Contrib.DuplicateRequestCollapser
{
	/// <summary>
	/// Provides a lock scoped to this instance of <see cref="InstanceScopedStripedLockProvider"/>.
	/// This lock provider will spread the locks per key, reducing contention and CPU usage, and
	/// is recommended for all use cases.<br/>
	/// This class uses about 4096 bytes of RAM. If you cannot afford this memory usage, please use
	/// <see cref="InstanceScopedLockProvider"/>.
	/// </summary>
	public class InstanceScopedStripedLockProvider : ISyncLockProvider
	{
		internal readonly int[] keyLocks = new int[1024];

		/// <inheritdoc/>
		public IDisposable AcquireLock(string key, Context context, CancellationToken cancellationToken)
		{
			return new InstanceScopedStripedLockReleaser(this, key);
		}

		private class InstanceScopedStripedLockReleaser : IDisposable
		{
			private InstanceScopedStripedLockProvider? _lockProvider;
			private uint _hash;

			/// <summary>
			/// Constructor
			/// </summary>
			/// <param name="provider">Lock provider</param>
			/// <param name="key">Key</param>
			public InstanceScopedStripedLockReleaser(InstanceScopedStripedLockProvider provider, string key)
			{
				AcquireLock(provider, key);
			}

			/// <inheritdoc />
			[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
			public void Dispose()
			{
				InstanceScopedStripedLockProvider? provider = _lockProvider;
				if (provider != null && Interlocked.CompareExchange(ref _lockProvider, null, provider) == provider)
				{
					provider.keyLocks[_hash] = 0;
				}
				// else we do not care, it can be disposed in an error case and we will simply ignore that the key locks were not touched
			}

			[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
			private void AcquireLock(InstanceScopedStripedLockProvider provider, string key)
			{
				// Monitor.Enter and Monitor.Exit are tied to a specific thread and are
				//  slower than this spin lock, which does not care about threads and will execute very
				//  quickly, regardless of lock contention
				// https://stackoverflow.com/questions/11001760/monitor-enter-and-monitor-exit-in-different-threads

				// make unchecked just in case compiler is doing extra checks in the int to uint cast
				unchecked
				{
					// Get a hash based on the key, use this to lock on a specific int in the array. The array is designed
					// to be small enough to not use very much memory, but large enough to avoid collisions.
					// Even if there is a collision, it will be resolved very quickly.
					_hash = (uint)(key ?? string.Empty).GetHashCode() % (uint)provider.keyLocks.Length;
				}

				// To get the lock, we must change the int at hash index from a 0 to a 1. If the value is
				//  already a 1, we don't get the lock. The return value must be 0 (the original value of the int).
				// it is very unlikely to have any contention here, but if so, the spin cycle should be very short.
				// Parameter index 1 (value of 1) is the value to change to if the existing value (Parameter index 2) is 0.
				while (Interlocked.CompareExchange(ref provider.keyLocks[_hash], 1, 0) == 1)
				{
					// give up a clock cycle, we want to get back and try to get the lock again very quickly
					System.Threading.Thread.Yield();
				}

				_lockProvider = provider;
			}
		}
	}
}

#nullable restore
