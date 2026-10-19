using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;

namespace Wyzely.Functional.MaybeBenchmarks;

// The receiver is a Task that is already completed and every delegate is synchronous: what does the async machinery cost?
// The benchmark methods return ValueTask<int> so that the harness itself allocates nothing. A library without extension methods for
// Task<Maybe<T>> is measured with the code its user would write instead: await the task, then run the synchronous chain.
[MemoryDiagnoser]
[CategoriesColumn]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
public partial class TaskReceiver
{
}
