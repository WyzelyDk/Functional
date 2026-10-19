using BenchmarkDotNet.Attributes;

namespace Wyzely.Functional.MaybeBenchmarks;

public partial class FromNullable
{
    [Benchmark(Baseline = true)]
    [BenchmarkCategory("ValuePresent")]
    public int Wyzely() => Maybe.From(_present).Match(static x => x, static () => -1);

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("ValueAbsent")]
    public int WyzelyAbsent() => Maybe.From(_absent).Match(static x => x, static () => -1);

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("ReferencePresent")]
    public int WyzelyRef() => Maybe.From(_presentText).Match(static s => s.Length, static () => -1);

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("ReferenceAbsent")]
    public int WyzelyRefAbsent() => Maybe.From(_absentText).Match(static s => s.Length, static () => -1);
}
