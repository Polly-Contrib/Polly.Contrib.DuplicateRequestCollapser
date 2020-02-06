using System;

namespace Polly.Contrib.DuplicateRequestCollapser
{
    /// <summary>
    /// The default key strategy.  Returns the property <see cref="M:Context.OperationKey"/> as the key.
    /// </summary>
    public class DefaultKeyStrategy : IKeyStrategy
    {
        /// <summary>
        /// Gets the key from the given execution context.
        /// </summary>
        /// <param name="context">The execution context.</param>
        /// <returns>The cache key</returns>
        public String GetKey(Context context) => context.OperationKey;

        /// <summary>
        /// Gets an instance of the <see cref="DefaultKeyStrategy"/>.
        /// </summary>
        public static readonly IKeyStrategy Instance = new DefaultKeyStrategy();
    }
}
