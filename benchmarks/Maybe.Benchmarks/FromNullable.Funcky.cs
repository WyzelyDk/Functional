using BenchmarkDotNet.Attributes;
using Funcky.Monads;

namespace Wyzely.Functional.MaybeBenchmarks;

public partial class FromNullable
{
    [Benchmark]
    [BenchmarkCategory("ValuePresent")]
    public int FunckyLib() => Option.FromNullable(_present).Match(none: static () => -1, some: static x => x);

    [Benchmark]
    [BenchmarkCategory("ValueAbsent")]
    public int FunckyAbsent() => Option.FromNullable(_absent).Match(none: static () => -1, some: static x => x);

    [Benchmark]
    [BenchmarkCategory("ReferencePresent")]
    public int FunckyRef() => Option.FromNullable(_presentText).Match(none: static () => -1, some: static s => s.Length);

    [Benchmark]
    [BenchmarkCategory("ReferenceAbsent")]
    public int FunckyRefAbsent() => Option.FromNullable(_absentText).Match(none: static () => -1, some: static s => s.Length);
}
