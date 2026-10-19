using BenchmarkDotNet.Attributes;
using OneOf;
using OneOf.Types;

namespace Wyzely.Functional.MaybeBenchmarks;

public partial class LongChain
{
    [Benchmark]
    [BenchmarkCategory("Value")]
    public int OneOfLib()
    {
        OneOf<int, None> start = _number;

        return start
            .MapT0(static x => x + 1)
            .Match(static x => x > 0 ? OneOfValue(x) : default(None), static none => none)
            .Match(static x => OneOfValue(x * 2), static none => none)
            .MapT0(static x => x - 3)
            .Match(static x => x != 0 ? OneOfValue(x) : default(None), static none => none)
            .Match(static x => OneOfValue(x + 10), static none => none)
            .MapT0(static x => x * 2)
            .Match(static x => x, static _ => -1);
    }

    [Benchmark]
    [BenchmarkCategory("Reference")]
    public int OneOfRef()
    {
        OneOf<Customer, None> start = _customer;

        return start
            .MapT0(static c => c.Name)
            .Match(static n => n.Length > 0 ? OneOfValue(n) : default(None), static none => none)
            .Match(static n => OneOfValue(n), static none => none)
            .MapT0(static n => n)
            .Match(static n => n[0] != ' ' ? OneOfValue(n) : default(None), static none => none)
            .Match(static n => OneOfValue(n), static none => none)
            .MapT0(static n => n)
            .Match(static n => n.Length, static _ => -1);
    }

    private static OneOf<T, None> OneOfValue<T>(T value) => value;
}
