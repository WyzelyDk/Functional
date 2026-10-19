using BenchmarkDotNet.Attributes;
using LanguageExt.Common;
using static LanguageExt.Prelude;

namespace Wyzely.Benchmarks;

public partial class LongChain
{
    private static readonly Error LangExtRejected = Error.New(Rejected);

    [Benchmark]
    [BenchmarkCategory("Value")]
    public int LangExt() => FinSucc(_number)
        .Map(static x => x + 1)
        .Bind(static x => x > 0 ? FinSucc(x) : FinFail<int>(LangExtRejected))
        .Bind(static x => FinSucc(x * 2))
        .Map(static x => x - 3)
        .Bind(static x => x != 0 ? FinSucc(x) : FinFail<int>(LangExtRejected))
        .Bind(static x => FinSucc(x + 10))
        .Map(static x => x * 2)
        .Match(static x => x, static _ => -1);

    [Benchmark]
    [BenchmarkCategory("Reference")]
    public int LangExtRef() => FinSucc(_customer)
        .Map(static c => c.Name)
        .Bind(static n => n.Length > 0 ? FinSucc(n) : FinFail<string>(LangExtRejected))
        .Bind(static n => FinSucc(n))
        .Map(static n => n)
        .Bind(static n => n[0] != ' ' ? FinSucc(n) : FinFail<string>(LangExtRejected))
        .Bind(static n => FinSucc(n))
        .Map(static n => n)
        .Match(static n => n.Length, static _ => -1);
}
