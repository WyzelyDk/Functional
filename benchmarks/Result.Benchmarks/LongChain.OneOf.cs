using BenchmarkDotNet.Attributes;
using OneOf;
using OneOf.Types;

namespace Wyzely.Benchmarks;

public partial class LongChain
{
    private static readonly Error<string> OneOfRejected = new(Rejected);

    [Benchmark]
    [BenchmarkCategory("Value")]
    public int OneOfLib()
    {
        OneOf<int, Error<string>> start = _number;

        return start
            .MapT0(static x => x + 1)
            .Match(static x => x > 0 ? OneOfValue(x) : OneOfRejected, static e => e)
            .Match(static x => OneOfValue(x * 2), static e => e)
            .MapT0(static x => x - 3)
            .Match(static x => x != 0 ? OneOfValue(x) : OneOfRejected, static e => e)
            .Match(static x => OneOfValue(x + 10), static e => e)
            .MapT0(static x => x * 2)
            .Match(static x => x, static _ => -1);
    }

    [Benchmark]
    [BenchmarkCategory("Reference")]
    public int OneOfRef()
    {
        OneOf<Customer, Error<string>> start = _customer;

        return start
            .MapT0(static c => c.Name)
            .Match(static n => n.Length > 0 ? OneOfValue(n) : OneOfRejected, static e => e)
            .Match(static n => OneOfValue(n), static e => e)
            .MapT0(static n => n)
            .Match(static n => n[0] != ' ' ? OneOfValue(n) : OneOfRejected, static e => e)
            .Match(static n => OneOfValue(n), static e => e)
            .MapT0(static n => n)
            .Match(static n => n.Length, static _ => -1);
    }

    private static OneOf<T, Error<string>> OneOfValue<T>(T value) => value;
}
