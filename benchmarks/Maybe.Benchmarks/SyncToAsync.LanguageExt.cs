using BenchmarkDotNet.Attributes;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Wyzely.Functional.MaybeBenchmarks;

public partial class SyncToAsync
{
    private static readonly Task<Option<int>> LangExtDone = Task.FromResult(Some(43));
    private static readonly Task<Option<string>> LangExtDoneRef = Task.FromResult(Some(Data.Customer.Name));

    [Benchmark]
    [BenchmarkCategory("Value")]
    public async ValueTask<int> LangExt()
    {
        var first = Some(_number);
        var second = first.IsSome ? await LangExtDone : Option<int>.None;

        return second.Map(static x => x + 1).Match(static x => x, static () => -1);
    }

    [Benchmark]
    [BenchmarkCategory("Reference")]
    public async ValueTask<int> LangExtRef()
    {
        var first = Some(_customer);
        var second = first.IsSome ? await LangExtDoneRef : Option<string>.None;

        return second.Match(static n => n.Length, static () => -1);
    }
}
