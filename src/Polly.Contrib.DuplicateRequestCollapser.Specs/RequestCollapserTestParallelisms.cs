using Xunit;

namespace Polly.Contrib.DuplicateRequestCollapser.Specs
{
    internal class RequestCollapserTestParallelisms : TheoryData<int>
    {
        public RequestCollapserTestParallelisms()
        {
            Add(2);
            Add(30);
        }
    }
}
