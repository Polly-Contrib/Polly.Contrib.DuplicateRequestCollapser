using System;
using System.Diagnostics.CodeAnalysis;

namespace Polly.Contrib.DuplicateRequestCollapser.Utils
{
    [ExcludeFromCodeCoverage]
    internal static class Guard
    {
        public static T NotNull<T>(T value, string argumentName = "")
            where T : class
        {
            if (value is null)
            {
                throw new ArgumentNullException(argumentName);
            }

            return value;
        }
    }
}
