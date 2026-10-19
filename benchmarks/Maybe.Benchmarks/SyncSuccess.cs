using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;

namespace Wyzely.Functional.MaybeBenchmarks;

// A synchronous chain of four steps that all keep their value: Map, Filter, Bind, Match (or the nearest equivalents of each library).
// The value chain runs on an int; the reference chain starts with a Customer and carries its name (a string) through the steps.
//
[MemoryDiagnoser]
[CategoriesColumn]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
public partial class SyncSuccess
{
    private readonly int _number = Data.Number;
    private readonly Customer _customer = Data.Customer;
}
