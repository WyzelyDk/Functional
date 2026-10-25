using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;

namespace Wyzely.Benchmarks;

// Combining eight results into one result with the eight values: all eight succeed, or the fifth one fails. A library without a combine method is measured
// with the loop its user would write (collect the values in a list, return at the first failure). The results exist in advance, so that the columns show
// the cost of combining and not of creating them. The value case combines ints; the reference case combines the name of a Customer (a string).
[MemoryDiagnoser]
[CategoriesColumn]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
public partial class Combine
{
    private const int Count = 8;
    private const int FailingIndex = 4;
}
