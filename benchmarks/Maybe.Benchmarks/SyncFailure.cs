using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;

namespace Wyzely.Functional.MaybeBenchmarks;

// The same chain as SyncSuccess, but the Filter step rejects the value, so the later steps are skipped and the none branch of Match
// runs.
[MemoryDiagnoser]
[CategoriesColumn]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
public partial class SyncFailure
{
    private readonly int _number = Data.Number;
    private readonly Customer _customer = Data.Customer;
}
