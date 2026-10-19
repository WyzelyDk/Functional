using BenchmarkDotNet.Attributes;
using LanguageExt;
using LanguageExt.Common;
using static LanguageExt.Prelude;

namespace Wyzely.Benchmarks;

public partial class ValueTaskReceiver
{
    private static readonly Error LangExtRejected = Error.New("Rejected.");

    // LanguageExt has no extension methods for ValueTask<Fin<A>>: await, then the synchronous chain.
    [Benchmark]
    [BenchmarkCategory("Value")]
    public async ValueTask<int> LangExt() => (await new ValueTask<Fin<int>>(FinSucc(_number)))
        .Map(static x => x + 1)
        .Bind(static x => x > 0 ? FinSucc(x) : FinFail<int>(LangExtRejected))
        .Bind(static x => FinSucc(x * 2))
        .Match(static x => x, static _ => -1);

    [Benchmark]
    [BenchmarkCategory("Reference")]
    public async ValueTask<int> LangExtRef() => (await new ValueTask<Fin<Customer>>(FinSucc(_customer)))
        .Map(static c => c.Name)
        .Bind(static n => n.Length > 0 ? FinSucc(n) : FinFail<string>(LangExtRejected))
        .Bind(static n => FinSucc(n))
        .Match(static n => n.Length, static _ => -1);
}
