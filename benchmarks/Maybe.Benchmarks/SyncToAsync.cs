using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;

namespace Wyzely.Functional.MaybeBenchmarks;

// The receiver is a plain maybe and the first step is an asynchronous delegate that returns an already completed, cached task, followed
// by a synchronous Map. What does it cost to go from a synchronous maybe into the asynchronous world? A library without a Bind that
// takes an asynchronous delegate is measured with the code its user would write instead: check the maybe, await the step.
[MemoryDiagnoser]
[CategoriesColumn]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
public partial class SyncToAsync
{
    private readonly int _number = Data.Number;
    private readonly Customer _customer = Data.Customer;
}
