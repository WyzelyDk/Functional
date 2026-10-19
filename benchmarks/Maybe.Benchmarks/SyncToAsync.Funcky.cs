using BenchmarkDotNet.Attributes;
using Funcky.Monads;

namespace Wyzely.Functional.MaybeBenchmarks;

public partial class SyncToAsync
{
    private static readonly Task<Option<int>> FunckyDone = Task.FromResult(Option.Some(43));
    private static readonly Task<Option<string>> FunckyDoneRef = Task.FromResult(Option.Some(Data.Customer.Name));

    [Benchmark]
    [BenchmarkCategory("Value")]
    public async ValueTask<int> FunckyLib()
    {
        var first = Option.Some(_number);
        var second = first.Match(none: static () => false, some: static _ => true) ? await FunckyDone : Option<int>.None;

        return second.Select(static x => x + 1).Match(none: static () => -1, some: static x => x);
    }

    [Benchmark]
    [BenchmarkCategory("Reference")]
    public async ValueTask<int> FunckyRef()
    {
        var first = Option.Some(_customer);
        var second = first.Match(none: static () => false, some: static _ => true) ? await FunckyDoneRef : Option<string>.None;

        return second.Match(none: static () => -1, some: static n => n.Length);
    }
}
