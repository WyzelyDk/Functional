using BenchmarkDotNet.Attributes;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Wyzely.Functional.MaybeBenchmarks;

public partial class TaskReceiver
{
    private readonly Task<Option<int>> _langExtTask = Task.FromResult(Some(Data.Number));
    private readonly Task<Option<Customer>> _langExtTaskRef = Task.FromResult(Some(Data.Customer));

    // LanguageExt does asynchronous work in a separate type, OptionAsync<A>, so Task<Option<A>> is awaited and the synchronous chain runs.
    [Benchmark]
    [BenchmarkCategory("Value")]
    public async ValueTask<int> LangExt() => (await _langExtTask)
        .Map(static x => x + 1)
        .Filter(static x => x > 0)
        .Bind(static x => Some(x * 2))
        .Match(static x => x, static () => -1);

    [Benchmark]
    [BenchmarkCategory("Reference")]
    public async ValueTask<int> LangExtRef() => (await _langExtTaskRef)
        .Map(static c => c.Name)
        .Filter(static n => n.Length > 0)
        .Bind(static n => Some(n))
        .Match(static n => n.Length, static () => -1);
}
