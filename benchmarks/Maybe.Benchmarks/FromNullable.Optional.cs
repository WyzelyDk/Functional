using BenchmarkDotNet.Attributes;
using Optional;

namespace Wyzely.Functional.MaybeBenchmarks;

public partial class FromNullable
{
    [Benchmark]
    [BenchmarkCategory("ValuePresent")]
    public int OptionalLib() => _present.ToOption().Match(static x => x, static () => -1);

    [Benchmark]
    [BenchmarkCategory("ValueAbsent")]
    public int OptionalAbsent() => _absent.ToOption().Match(static x => x, static () => -1);

    [Benchmark]
    [BenchmarkCategory("ReferencePresent")]
    public int OptionalRef() => _presentText.SomeNotNull().Match(static s => s!.Length, static () => -1);

    [Benchmark]
    [BenchmarkCategory("ReferenceAbsent")]
    public int OptionalRefAbsent() => _absentText.SomeNotNull().Match(static s => s!.Length, static () => -1);
}
