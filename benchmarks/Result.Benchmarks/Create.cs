using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;

namespace Wyzely.Benchmarks;

// A method creates a successful result and returns it, and the caller reads the value back: what does it cost to hand a result out of
// a method? The creating method is not inlined, as in real code where a result crosses a method boundary; inlined, the compiler removes
// the result type of the struct libraries altogether.
[MemoryDiagnoser]
[CategoriesColumn]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
public partial class Create
{
    private readonly int _number = Data.Number;
    private readonly Customer _customer = Data.Customer;
}
