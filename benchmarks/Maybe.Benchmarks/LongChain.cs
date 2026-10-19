using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;

namespace Wyzely.Functional.MaybeBenchmarks;

// A synchronous chain of eight steps that all keep their value (Map, Filter, Bind, Map, Filter, Bind, Map, Match, or the nearest
// equivalents): does the cost of a maybe type grow with the number of steps?
[MemoryDiagnoser]
[CategoriesColumn]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
public partial class LongChain
{
    private readonly int _number = Data.Number;
    private readonly Customer _customer = Data.Customer;
}
