using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;

namespace Wyzely.Functional.MaybeBenchmarks;

// Turning a value that may be missing (an int? or a string reference that may be null) into a maybe and reading it back: the
// boundary with code that uses null. There are four cases: a value type and a reference type, each present and absent.
// OneOf has no counterpart, as a null is no value of a union.
[MemoryDiagnoser]
[CategoriesColumn]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
public partial class FromNullable
{
    private readonly int? _present = Data.Number;
    private readonly int? _absent;
    private readonly string? _presentText = Data.Customer.Name;
    private readonly string? _absentText;

    public FromNullable()
    {
        _absent = null;
        _absentText = null;
    }
}
