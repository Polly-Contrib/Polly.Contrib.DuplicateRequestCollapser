namespace Polly.Contrib.DuplicateRequestCollapser
{
    /// <summary>
    /// Defines properties and methods common to all RequestCollapser policies.
    /// </summary>

    public interface IRequestCollapserPolicy : IsPolicy
    {
    }

    /// <summary>
    /// Defines properties and methods common to all RequestCollapser policies generic-typed for executions returning results of type <typeparamref name="TResult"/>.
    /// </summary>
    public interface IRequestCollapserPolicy<TResult> : IRequestCollapserPolicy
    {
    }
}
