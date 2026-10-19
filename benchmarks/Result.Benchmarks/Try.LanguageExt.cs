using BenchmarkDotNet.Attributes;
using static LanguageExt.Prelude;

namespace Wyzely.Benchmarks;

public partial class Try
{
    [Benchmark]
    [BenchmarkCategory("ValueSuccess")]
    public int LangExt() => Try(static () => Parse("21")).IfFail(-1);

    [Benchmark]
    [BenchmarkCategory("ValueFailure")]
    public int LangExtFailure() => Try(static () => Parse("x")).IfFail(-1);

    [Benchmark]
    [BenchmarkCategory("ReferenceSuccess")]
    public int LangExtRef() => Try(static () => Find("ada")).Match(static c => c.Name.Length, static _ => -1);

    [Benchmark]
    [BenchmarkCategory("ReferenceFailure")]
    public int LangExtRefFailure() => Try(static () => Find(string.Empty)).Match(static c => c.Name.Length, static _ => -1);
}
