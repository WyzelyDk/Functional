using BenchmarkDotNet.Attributes;
using LanguageExt.Common;
using static LanguageExt.Prelude;

namespace Wyzely.Benchmarks;

public partial class Conditional
{
    private static readonly Error LangExtRejected = Error.New("Rejected.");

    // Fin<A> has no conditional methods and no Ensure: the conditions are conditional expressions, and the check is a Bind step.
    [Benchmark]
    [BenchmarkCategory("Value")]
    public int LangExt() => FinSucc(_number)
        .Map(static x => x > 0 ? x + 1 : x)
        .Bind(static x => x > 1 ? FinSucc(x * 2) : FinSucc(x))
        .Bind(static x => x > 0 ? FinSucc(x) : FinFail<int>(LangExtRejected))
        .Match(static x => x, static _ => -1);

    [Benchmark]
    [BenchmarkCategory("Reference")]
    public int LangExtRef() => FinSucc(_name)
        .Map(static n => n.Length > 0 ? n.Trim() : n)
        .Bind(static n => n.Length > 1 ? FinSucc(n.TrimEnd()) : FinSucc(n))
        .Bind(static n => n.Length > 0 ? FinSucc(n) : FinFail<string>(LangExtRejected))
        .Match(static n => n.Length, static _ => -1);
}
