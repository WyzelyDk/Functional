using BenchmarkDotNet.Attributes;
using OneOf;
using OneOf.Types;

namespace Wyzely.Functional.MaybeBenchmarks;

public partial class Predicate
{
    // OneOf is a union, not a railway library: OneOf<T, None> stands for the maybe, and the question is a Match.
    [Benchmark]
    [BenchmarkCategory("ValueSome")]
    public bool OneOfLib() => ToOneOf(_number).Match(static x => x > 0, static _ => false);

    [Benchmark]
    [BenchmarkCategory("ValueNone")]
    public bool OneOfLibNone() => ToNone<int>().Match(static x => x > 0, static _ => false);

    [Benchmark]
    [BenchmarkCategory("ReferenceSome")]
    public bool OneOfLibRef() => ToOneOf(_name).Match(static n => n.Length > 0, static _ => false);

    [Benchmark]
    [BenchmarkCategory("ReferenceNone")]
    public bool OneOfLibRefNone() => ToNone<string>().Match(static n => n.Length > 0, static _ => false);

    private static OneOf<T, None> ToOneOf<T>(T value) => value;

    private static OneOf<T, None> ToNone<T>() => default(None);
}
