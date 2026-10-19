using BenchmarkDotNet.Attributes;
using OneOf;
using OneOf.Types;

namespace Wyzely.Functional.MaybeBenchmarks;

public partial class SyncFailure
{
    // OneOf is a union, not a railway library: OneOf<T, None> stands for the maybe, only MapT0 and Match exist, and Filter and Bind are
    // written with Match.
    [Benchmark]
    [BenchmarkCategory("Value")]
    public int OneOfLib()
    {
        OneOf<int, None> start = _number;

        return start
            .MapT0(static x => x + 1)
            .Match(static x => x < 0 ? OneOfValue(x) : default(None), static none => none)
            .Match(static x => OneOfValue(x * 2), static none => none)
            .Match(static x => x, static _ => -1);
    }

    [Benchmark]
    [BenchmarkCategory("Reference")]
    public int OneOfRef()
    {
        OneOf<Customer, None> start = _customer;

        return start
            .MapT0(static c => c.Name)
            .Match(static n => n.Length > 100 ? OneOfValue(n) : default(None), static none => none)
            .Match(static n => OneOfValue(n), static none => none)
            .Match(static n => n.Length, static _ => -1);
    }

    private static OneOf<T, None> OneOfValue<T>(T value) => value;
}
