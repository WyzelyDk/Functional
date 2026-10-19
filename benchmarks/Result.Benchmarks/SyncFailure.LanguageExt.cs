using BenchmarkDotNet.Attributes;
using LanguageExt.Common;
using static LanguageExt.Prelude;

namespace Wyzely.Benchmarks;

public partial class SyncFailure
{
    private static readonly Error LangExtRejected = Error.New("Rejected.");

    [Benchmark]
    [BenchmarkCategory("Value")]
    public int LangExt() => FinSucc(_number)
        .Map(static x => x + 1)
        .Bind(static x => x < 0 ? FinSucc(x) : FinFail<int>(LangExtRejected))
        .Bind(static x => FinSucc(x * 2))
        .Match(static x => x, static _ => -1);

    [Benchmark]
    [BenchmarkCategory("Reference")]
    public int LangExtRef() => FinSucc(_customer)
        .Map(static c => c.Name)
        .Bind(static n => n.Length > 100 ? FinSucc(n) : FinFail<string>(LangExtRejected))
        .Bind(static n => FinSucc(n))
        .Match(static n => n.Length, static _ => -1);
}
