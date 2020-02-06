using System;

namespace Polly.Contrib.DuplicateRequestCollapser
{
    public partial class RequestCollapserPolicy<TResult>
    {
        /// <summary>
        /// Builds a <see cref="Polly.Contrib.DuplicateRequestCollapser.RequestCollapserPolicy{TResult}"/>, using the <see cref="DefaultKeyStrategy"/>.
        /// </summary>
        /// <returns>The policy instance.</returns>
        public static ISyncRequestCollapserPolicy<TResult> Create()
            => Create(RequestCollapserPolicy.DefaultKeyStrategy, RequestCollapserPolicy.GetDefaultLockProvider());

        /// <summary>
        /// Creates a new <see cref="Polly.Contrib.DuplicateRequestCollapser.RequestCollapserPolicy{TResult}"/> policy, using the supplied <see cref="ISyncLockProvider"/>
        /// </summary>
        /// <param name="lockProvider">The lock provider.</param>
        /// <returns>The policy instance.</returns>
        public static ISyncRequestCollapserPolicy<TResult> Create(ISyncLockProvider lockProvider)
            => Create(RequestCollapserPolicy.DefaultKeyStrategy, lockProvider);

        /// <summary>
        /// Creates a new <see cref="Polly.Contrib.DuplicateRequestCollapser.RequestCollapserPolicy{TResult}"/> policy, using the supplied <see cref="IKeyStrategy"/>
        /// </summary>
        /// <param name="keyStrategy">A strategy for choosing a key on which to consider requests duplicates.</param>
        /// <returns>The policy instance.</returns>
        public static ISyncRequestCollapserPolicy<TResult> Create(IKeyStrategy keyStrategy)
            => Create(keyStrategy, RequestCollapserPolicy.GetDefaultLockProvider());

        /// <summary>
        /// Creates a new <see cref="Polly.Contrib.DuplicateRequestCollapser.RequestCollapserPolicy{TResult}"/> policy, using the supplied <see cref="IKeyStrategy"/> and <see cref="ISyncLockProvider"/>
        /// </summary>
        /// <param name="keyStrategy">A strategy for choosing a key on which to consider requests duplicates.</param>
        /// <param name="lockProvider">The lock provider.</param>
        /// <returns>The policy instance.</returns>
        public static ISyncRequestCollapserPolicy<TResult> Create(IKeyStrategy keyStrategy, ISyncLockProvider lockProvider)
        {
            if (keyStrategy == null) throw new ArgumentNullException(nameof(keyStrategy));
            if (lockProvider == null) throw new ArgumentNullException(nameof(lockProvider));

            return new RequestCollapserPolicy<TResult>(keyStrategy, lockProvider);
        }
    }
}
