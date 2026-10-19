using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;

namespace Wyzely.Benchmarks;

// A synchronous chain of eight steps that all succeed (Map, Ensure, Bind, Map, Ensure, Bind, Map, Match, or the nearest equivalents):
// does the cost of a result type grow with the number of steps?
[MemoryDiagnoser]
[CategoriesColumn]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
public partial class LongChain
{
    private const string Rejected = "Rejected.";

    private readonly int _number = Data.Number;
    private readonly Customer _customer = Data.Customer;
}
