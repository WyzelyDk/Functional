using BenchmarkDotNet.Attributes;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Wyzely.Benchmarks;

public partial class SyncToAsync
{
    private static readonly Task<Fin<int>> LangExtDone = Task.FromResult(FinSucc(43));
    private static readonly Task<Fin<string>> LangExtDoneRef = Task.FromResult(FinSucc(Data.Customer.Name));

    [Benchmark]
    [BenchmarkCategory("Value")]
    public async ValueTask<int> LangExt()
    {
        var first = FinSucc(_number);
        var second = first.IsSucc ? await LangExtDone : first;

        return second.Match(static x => x + 1, static _ => -1);
    }

    [Benchmark]
    [BenchmarkCategory("Reference")]
    public async ValueTask<int> LangExtRef()
    {
        var first = FinSucc(_customer);
        var second = first.IsSucc ? await LangExtDoneRef : FinFail<string>(LanguageExt.Common.Error.New("Rejected."));

        return second.Match(static n => n.Length, static _ => -1);
    }
}
