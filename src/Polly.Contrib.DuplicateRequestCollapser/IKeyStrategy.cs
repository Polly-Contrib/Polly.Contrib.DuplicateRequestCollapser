namespace Polly.Contrib.DuplicateRequestCollapser;

/// <summary>
/// Defines how a policy should get a key from a policy execution <see cref="ResilienceContext"/>
/// </summary>
public interface IKeyStrategy
{
    /// <summary>
    /// Gets a string key, which may depend on the passed execution context.
    /// </summary>
    /// <param name="context">The execution context.</param>
    /// <returns>The key</returns>
    string? GetKey(ResilienceContext context);
}
