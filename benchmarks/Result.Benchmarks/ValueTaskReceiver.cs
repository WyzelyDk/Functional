using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;

namespace Wyzely.Benchmarks;

// The receiver is a ValueTask that is already completed and every delegate is synchronous. ValueTask exists to avoid allocations
// when work completes synchronously, so this should allocate nothing. A library without extension methods for ValueTask<Result<T>>
// is measured with the code its user would write instead: await the value task, then run the synchronous chain.
[MemoryDiagnoser]
[CategoriesColumn]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
public partial class ValueTaskReceiver
{
    private readonly int _number = Data.Number;
    private readonly Customer _customer = Data.Customer;
}
