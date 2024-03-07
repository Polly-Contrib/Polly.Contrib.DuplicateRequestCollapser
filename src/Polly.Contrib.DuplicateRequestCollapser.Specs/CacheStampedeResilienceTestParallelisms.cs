using Xunit;

namespace Polly.Contrib.DuplicateRequestCollapser.Specs
{
    internal class CacheStampedeResilienceTestParallelisms : TheoryData<int>
    {
        public CacheStampedeResilienceTestParallelisms()
        {
            Add(2);
            Add(30);
        }
    }
}
