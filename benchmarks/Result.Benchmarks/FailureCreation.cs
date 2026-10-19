using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;

namespace Wyzely.Benchmarks;

// Creating a failed result with a new error, as code does when a rule is broken, and asking whether it failed. The payload type only
// decides which result type is created.
[MemoryDiagnoser]
[CategoriesColumn]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
public partial class FailureCreation
{
}
