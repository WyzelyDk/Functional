using BenchmarkDotNet.Attributes;
using OneOf;
using OneOf.Types;

namespace Wyzely.Benchmarks;

public partial class Conditional
{
    // OneOf is a union, not a railway library: only MapT0 and Match exist, so the conditions and the check are written with Match.
    [Benchmark]
    [BenchmarkCategory("Value")]
    public int OneOfLib()
    {
        OneOf<int, Error<string>> start = _number;

        return start
            .MapT0(static x => x > 0 ? x + 1 : x)
            .Match(static x => x > 1 ? OneOfValue(x * 2) : OneOfValue(x), static e => e)
            .Match(static x => x > 0 ? OneOfValue(x) : OneOfRejected<int>(), static e => e)
            .Match(static x => x, static _ => -1);
    }

    [Benchmark]
    [BenchmarkCategory("Reference")]
    public int OneOfRef()
    {
        OneOf<string, Error<string>> start = _name;

        return start
            .MapT0(static n => n.Length > 0 ? n.Trim() : n)
            .Match(static n => n.Length > 1 ? OneOfValue(n.TrimEnd()) : OneOfValue(n), static e => e)
            .Match(static n => n.Length > 0 ? OneOfValue(n) : OneOfRejected<string>(), static e => e)
            .Match(static n => n.Length, static _ => -1);
    }

    private static OneOf<T, Error<string>> OneOfValue<T>(T value) => value;

    private static OneOf<T, Error<string>> OneOfRejected<T>() => new Error<string>("Rejected.");
}
