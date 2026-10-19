using BenchmarkDotNet.Attributes;
using static LanguageExt.Prelude;

namespace Wyzely.Functional.MaybeBenchmarks;

public partial class LongChain
{
    [Benchmark]
    [BenchmarkCategory("Value")]
    public int LangExt() => Some(_number)
        .Map(static x => x + 1)
        .Filter(static x => x > 0)
        .Bind(static x => Some(x * 2))
        .Map(static x => x - 3)
        .Filter(static x => x != 0)
        .Bind(static x => Some(x + 10))
        .Map(static x => x * 2)
        .Match(static x => x, static () => -1);

    [Benchmark]
    [BenchmarkCategory("Reference")]
    public int LangExtRef() => Some(_customer)
        .Map(static c => c.Name)
        .Filter(static n => n.Length > 0)
        .Bind(static n => Some(n))
        .Map(static n => n)
        .Filter(static n => n[0] != ' ')
        .Bind(static n => Some(n))
        .Map(static n => n)
        .Match(static n => n.Length, static () => -1);
}
