using BenchmarkDotNet.Attributes;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Wyzely.Functional.MaybeBenchmarks;

public partial class Predicate
{
    // Option<A> has Exists.
    [Benchmark]
    [BenchmarkCategory("ValueSome")]
    public bool LangExt() => Some(_number).Exists(static x => x > 0);

    [Benchmark]
    [BenchmarkCategory("ValueNone")]
    public bool LangExtNone() => Option<int>.None.Exists(static x => x > 0);

    [Benchmark]
    [BenchmarkCategory("ReferenceSome")]
    public bool LangExtRef() => Some(_name).Exists(static n => n.Length > 0);

    [Benchmark]
    [BenchmarkCategory("ReferenceNone")]
    public bool LangExtRefNone() => Option<string>.None.Exists(static n => n.Length > 0);
}
