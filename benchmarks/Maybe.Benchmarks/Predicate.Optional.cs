using BenchmarkDotNet.Attributes;
using Optional;

namespace Wyzely.Functional.MaybeBenchmarks;

public partial class Predicate
{
    // Option<T> has Exists.
    [Benchmark]
    [BenchmarkCategory("ValueSome")]
    public bool OptionalLib() => Option.Some(_number).Exists(static x => x > 0);

    [Benchmark]
    [BenchmarkCategory("ValueNone")]
    public bool OptionalLibNone() => Option.None<int>().Exists(static x => x > 0);

    [Benchmark]
    [BenchmarkCategory("ReferenceSome")]
    public bool OptionalLibRef() => Option.Some(_name).Exists(static n => n.Length > 0);

    [Benchmark]
    [BenchmarkCategory("ReferenceNone")]
    public bool OptionalLibRefNone() => Option.None<string>().Exists(static n => n.Length > 0);
}
