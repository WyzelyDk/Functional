using BenchmarkDotNet.Attributes;

namespace Wyzely.Functional.MaybeBenchmarks;

public partial class FromNullable
{
    [Benchmark]
    [BenchmarkCategory("ValuePresent")]
    public int LangExt() => LanguageExt.Prelude.Optional(_present).Match(static x => x, static () => -1);

    [Benchmark]
    [BenchmarkCategory("ValueAbsent")]
    public int LangExtAbsent() => LanguageExt.Prelude.Optional(_absent).Match(static x => x, static () => -1);

    [Benchmark]
    [BenchmarkCategory("ReferencePresent")]
    public int LangExtRef() => LanguageExt.Prelude.Optional(_presentText).Match(static s => s.Length, static () => -1);

    [Benchmark]
    [BenchmarkCategory("ReferenceAbsent")]
    public int LangExtRefAbsent() => LanguageExt.Prelude.Optional(_absentText).Match(static s => s.Length, static () => -1);
}
