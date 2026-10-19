using BenchmarkDotNet.Attributes;
using LanguageExt;
using LanguageExt.Common;
using static LanguageExt.Prelude;

namespace Wyzely.Benchmarks;

public partial class TaskReceiver
{
    private static readonly Error LangExtRejected = Error.New("Rejected.");

    private readonly Task<Fin<int>> _langExtTask = Task.FromResult(FinSucc(Data.Number));
    private readonly Task<Fin<Customer>> _langExtTaskRef = Task.FromResult(FinSucc(Data.Customer));

    // LanguageExt has no extension methods for Task<Fin<A>>: the task is awaited and the synchronous chain runs on the result.
    [Benchmark]
    [BenchmarkCategory("Value")]
    public async ValueTask<int> LangExt() => (await _langExtTask)
        .Map(static x => x + 1)
        .Bind(static x => x > 0 ? FinSucc(x) : FinFail<int>(LangExtRejected))
        .Bind(static x => FinSucc(x * 2))
        .Match(static x => x, static _ => -1);

    [Benchmark]
    [BenchmarkCategory("Reference")]
    public async ValueTask<int> LangExtRef() => (await _langExtTaskRef)
        .Map(static c => c.Name)
        .Bind(static n => n.Length > 0 ? FinSucc(n) : FinFail<string>(LangExtRejected))
        .Bind(static n => FinSucc(n))
        .Match(static n => n.Length, static _ => -1);
}
