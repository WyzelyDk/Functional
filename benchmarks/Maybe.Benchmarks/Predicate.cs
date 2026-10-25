using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;

namespace Wyzely.Functional.MaybeBenchmarks;

// Asking whether the value of a maybe satisfies a condition, for a maybe that has a value (and the condition holds) and for none. Wyzely has Any; the other
// libraries are measured with their nearest equivalent (Exists, or Where followed by a test for a value). The reference case asks about the name of a Customer.
[MemoryDiagnoser]
[CategoriesColumn]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
public partial class Predicate
{
    private readonly int _number = Data.Number;
    private readonly string _name = Data.Customer.Name;
}
