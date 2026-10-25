using BenchmarkDotNet.Attributes;
using CSharpFunctionalExtensions;
using CsfeInt = CSharpFunctionalExtensions.Maybe<int>;
using CsfeString = CSharpFunctionalExtensions.Maybe<string>;

namespace Wyzely.Functional.MaybeBenchmarks;

public partial class Predicate
{
    // CSharpFunctionalExtensions has no Any or Exists on a Maybe: Where keeps the value when the condition holds, and HasValue asks.
    [Benchmark]
    [BenchmarkCategory("ValueSome")]
    public bool Csfe() => CsfeInt.From(_number).Where(static x => x > 0).HasValue;

    [Benchmark]
    [BenchmarkCategory("ValueNone")]
    public bool CsfeNone() => CsfeInt.None.Where(static x => x > 0).HasValue;

    [Benchmark]
    [BenchmarkCategory("ReferenceSome")]
    public bool CsfeRef() => CsfeString.From(_name).Where(static n => n.Length > 0).HasValue;

    [Benchmark]
    [BenchmarkCategory("ReferenceNone")]
    public bool CsfeRefNone() => CsfeString.None.Where(static n => n.Length > 0).HasValue;
}
