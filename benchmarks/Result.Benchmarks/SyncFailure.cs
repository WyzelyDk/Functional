using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;

namespace Wyzely.Benchmarks;

// The same chain as SyncSuccess, but the Ensure step fails, so the later steps are skipped and the error is matched. The error exists
// already (a static field or a literal), so this measures propagation and not the creation of an error.
[MemoryDiagnoser]
[CategoriesColumn]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
public partial class SyncFailure
{
    private readonly int _number = Data.Number;
    private readonly Customer _customer = Data.Customer;
}
