using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;

namespace Wyzely.Benchmarks;

// A synchronous chain of four steps that all keep their value: a conditional Map, a conditional Bind, a check, and Match. The three conditions hold, so every
// step runs. A library without conditional methods is measured with the conditional expression its user would write inside Map and Bind.
// The value chain runs on an int; the reference chain starts with the name of a Customer (a string) and carries it on. Trim and TrimEnd return the same
// string when there is nothing to trim, so the allocation columns show what the result type costs and not what the payload costs.
//
[MemoryDiagnoser]
[CategoriesColumn]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
public partial class Conditional
{
    private readonly int _number = Data.Number;
    private readonly string _name = Data.Customer.Name;
}
