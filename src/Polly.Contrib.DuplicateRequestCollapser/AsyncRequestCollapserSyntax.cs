using System;

namespace Polly.Contrib.DuplicateRequestCollapser
{
    public partial class AsyncRequestCollapserPolicy
    {
        /// <summary>
        /// Builds a <see cref="Polly.Contrib.DuplicateRequestCollapser.RequestCollapserPolicy"/>, using the <see cref="DefaultKeyStrategy"/>.
        /// </summary>
        /// <returns>The policy instance.</returns>
        public static IAsyncRequestCollapserPolicy Create()
            => Create(RequestCollapserPolicy.DefaultKeyStrategy, AsyncRequestCollapserPolicy.GetDefaultLockProvider());

        /// <summary>
        /// Creates a new <see cref="Polly.Contrib.DuplicateRequestCollapser.AsyncRequestCollapserPolicy"/>, using the supplied <see cref="ISyncLockProvider"/>
        /// </summary>
        /// <param name="lockProvider">The lock provider.</param>
        /// <returns>The policy instance.</returns>
        public static IAsyncRequestCollapserPolicy Create(ISyncLockProvider lockProvider)
            => Create(RequestCollapserPolicy.DefaultKeyStrategy, lockProvider);

        /// <summary>
        /// Creates a new <see cref="Polly.Contrib.DuplicateRequestCollapser.AsyncRequestCollapserPolicy"/>, using the supplied <see cref="IAsyncLockProvider"/>
        /// </summary>
        /// <param name="lockProvider">The lock provider.</param>
        /// <returns>The policy instance.</returns>
        public static IAsyncRequestCollapserPolicy Create(IAsyncLockProvider lockProvider)
            => Create(RequestCollapserPolicy.DefaultKeyStrategy, lockProvider);

        /// <summary>
        /// Creates a new <see cref="Polly.Contrib.DuplicateRequestCollapser.AsyncRequestCollapserPolicy"/>, using the supplied <see cref="IKeyStrategy"/>
        /// </summary>
        /// <param name="keyStrategy">A strategy for choosing a key on which to consider requests duplicates.</param>
        /// <returns>The policy instance.</returns>
        public static IAsyncRequestCollapserPolicy Create(IKeyStrategy keyStrategy)
            => Create(keyStrategy, AsyncRequestCollapserPolicy.GetDefaultLockProvider());

        /// <summary>
        /// Creates a new <see cref="Polly.Contrib.DuplicateRequestCollapser.AsyncRequestCollapserPolicy"/>, using the supplied <see cref="IKeyStrategy"/> and <see cref="ISyncLockProvider"/>
        /// </summary>
        /// <param name="keyStrategy">A strategy for choosing a key on which to consider requests duplicates.</param>
        /// <param name="lockProvider">The lock provider.</param>
        /// <returns>The policy instance.</returns>
        public static IAsyncRequestCollapserPolicy Create(IKeyStrategy keyStrategy, ISyncLockProvider lockProvider)
            => Create(keyStrategy, new AsyncWrapperLockProvider(lockProvider));

        /// <summary>
        /// Creates a new <see cref="Polly.Contrib.DuplicateRequestCollapser.AsyncRequestCollapserPolicy"/>, using the supplied <see cref="IKeyStrategy"/> and <see cref="IAsyncLockProvider"/>
        /// </summary>
        /// <param name="keyStrategy">A strategy for choosing a key on which to consider requests duplicates.</param>
        /// <param name="lockProvider">The lock provider.</param>
        /// <returns>The policy instance.</returns>
        public static IAsyncRequestCollapserPolicy Create(IKeyStrategy keyStrategy, IAsyncLockProvider lockProvider)
        {
            if (keyStrategy == null) throw new ArgumentNullException(nameof(keyStrategy));
            if (lockProvider == null) throw new ArgumentNullException(nameof(lockProvider));

            return new AsyncRequestCollapserPolicy(keyStrategy, lockProvider);
        }
    }
}
