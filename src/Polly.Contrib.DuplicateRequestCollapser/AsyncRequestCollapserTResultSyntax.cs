using System;

namespace Polly.Contrib.DuplicateRequestCollapser
{
    public partial class AsyncRequestCollapserPolicy<TResult>
    {
        /// <summary>
        /// Builds a <see cref="Polly.Contrib.DuplicateRequestCollapser.AsyncRequestCollapserPolicy{TResult}"/>, using the <see cref="DefaultKeyStrategy"/>.
        /// </summary>
        /// <returns>The policy instance.</returns>
        public static IAsyncRequestCollapserPolicy<TResult> Create()
            => Create(RequestCollapserPolicy.DefaultKeyStrategy, AsyncRequestCollapserPolicy.GetDefaultLockProvider());

        /// <summary>
        /// Creates a new <see cref="Polly.Contrib.DuplicateRequestCollapser.AsyncRequestCollapserPolicy{TResult}"/> policy, using the supplied <see cref="ISyncLockProvider"/>
        /// </summary>
        /// <param name="lockProvider">The lock provider.</param>
        /// <returns>The policy instance.</returns>
        public static IAsyncRequestCollapserPolicy<TResult> Create(ISyncLockProvider lockProvider)
            => Create(RequestCollapserPolicy.DefaultKeyStrategy, lockProvider);

        /// <summary>
        /// Creates a new <see cref="Polly.Contrib.DuplicateRequestCollapser.AsyncRequestCollapserPolicy{TResult}"/> policy, using the supplied <see cref="IAsyncLockProvider"/>
        /// </summary>
        /// <param name="lockProvider">The lock provider.</param>
        /// <returns>The policy instance.</returns>
        public static IAsyncRequestCollapserPolicy<TResult> Create(IAsyncLockProvider lockProvider)
            => Create(RequestCollapserPolicy.DefaultKeyStrategy, lockProvider);

        /// <summary>
        /// Creates a new <see cref="Polly.Contrib.DuplicateRequestCollapser.AsyncRequestCollapserPolicy{TResult}"/> policy, using the supplied <see cref="IKeyStrategy"/>
        /// </summary>
        /// <param name="keyStrategy">A strategy for choosing a key on which to consider requests duplicates.</param>
        /// <returns>The policy instance.</returns>
        public static IAsyncRequestCollapserPolicy<TResult> Create(IKeyStrategy keyStrategy)
            => Create(keyStrategy, AsyncRequestCollapserPolicy.GetDefaultLockProvider());

        /// <summary>
        /// Creates a new <see cref="Polly.Contrib.DuplicateRequestCollapser.AsyncRequestCollapserPolicy{TResult}"/> policy, using the supplied <see cref="IKeyStrategy"/> and <see cref="ISyncLockProvider"/>
        /// </summary>
        /// <param name="keyStrategy">A strategy for choosing a key on which to consider requests duplicates.</param>
        /// <param name="lockProvider">The lock provider.</param>
        /// <returns>The policy instance.</returns>
        public static IAsyncRequestCollapserPolicy<TResult> Create(IKeyStrategy keyStrategy, ISyncLockProvider lockProvider)
            => Create(keyStrategy, new AsyncWrapperLockProvider(lockProvider));

        /// <summary>
        /// Creates a new <see cref="Polly.Contrib.DuplicateRequestCollapser.AsyncRequestCollapserPolicy{TResult}"/> policy, using the supplied <see cref="IKeyStrategy"/> and <see cref="IAsyncLockProvider"/>
        /// </summary>
        /// <param name="keyStrategy">A strategy for choosing a key on which to consider requests duplicates.</param>
        /// <param name="lockProvider">The lock provider.</param>
        /// <returns>The policy instance.</returns>
        public static IAsyncRequestCollapserPolicy<TResult> Create(IKeyStrategy keyStrategy, IAsyncLockProvider lockProvider)
        {
            if (keyStrategy == null) throw new ArgumentNullException(nameof(keyStrategy));
            if (lockProvider == null) throw new ArgumentNullException(nameof(lockProvider));

            return new AsyncRequestCollapserPolicy<TResult>(keyStrategy, lockProvider);
        }
    }
}
