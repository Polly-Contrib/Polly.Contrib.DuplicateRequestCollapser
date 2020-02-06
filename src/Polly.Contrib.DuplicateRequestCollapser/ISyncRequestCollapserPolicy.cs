namespace Polly.Contrib.DuplicateRequestCollapser
{
    /// <summary>
    /// A concurrent duplicate request collapser policy that can be applied to synchronous executions.  Code executed through the policy is executed as if no policy was applied.
    /// </summary>
    public interface ISyncRequestCollapserPolicy : ISyncPolicy, IRequestCollapserPolicy
    {
    }

    /// <summary>
    /// A concurrent duplicate request collapser policy that can be applied to synchronous executions returning a value of type <typeparamref name="TResult"/>.  Code executed through the policy is executed as if no policy was applied.
    /// </summary>
    /// <typeparam name="TResult">The return type of delegates which may be executed through the policy.</typeparam>
    public interface ISyncRequestCollapserPolicy<TResult> : ISyncPolicy<TResult>, IRequestCollapserPolicy<TResult>
    {
    }
}
