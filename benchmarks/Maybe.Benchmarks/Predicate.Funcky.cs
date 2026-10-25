using BenchmarkDotNet.Attributes;
using Funcky.Monads;

namespace Wyzely.Functional.MaybeBenchmarks;

public partial class Predicate
{
    // Funcky has no Any or Exists on an Option: Where keeps the value when the condition holds, and Match asks.
    [Benchmark]
    [BenchmarkCategory("ValueSome")]
    public bool FunckyLib() => Option.Some(_number).Where(static x => x > 0).Match(none: static () => false, some: static _ => true);

    [Benchmark]
    [BenchmarkCategory("ValueNone")]
    public bool FunckyLibNone() => Option<int>.None.Where(static x => x > 0).Match(none: static () => false, some: static _ => true);

    [Benchmark]
    [BenchmarkCategory("ReferenceSome")]
    public bool FunckyLibRef() => Option.Some(_name).Where(static n => n.Length > 0).Match(none: static () => false, some: static _ => true);

    [Benchmark]
    [BenchmarkCategory("ReferenceNone")]
    public bool FunckyLibRefNone() => Option<string>.None.Where(static n => n.Length > 0).Match(none: static () => false, some: static _ => true);
}
