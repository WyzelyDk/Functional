using BenchmarkDotNet.Attributes;
using Optional;

namespace Wyzely.Functional.MaybeBenchmarks;

public partial class SyncToAsync
{
    private static readonly Task<Option<int>> OptionalDone = Task.FromResult(Option.Some(43));
    private static readonly Task<Option<string>> OptionalDoneRef = Task.FromResult(Option.Some(Data.Customer.Name));

    [Benchmark]
    [BenchmarkCategory("Value")]
    public async ValueTask<int> OptionalLib()
    {
        var first = Option.Some(_number);
        var second = first.HasValue ? await OptionalDone : Option.None<int>();

        return second.Map(static x => x + 1).ValueOr(-1);
    }

    [Benchmark]
    [BenchmarkCategory("Reference")]
    public async ValueTask<int> OptionalRef()
    {
        var first = Option.Some(_customer);
        var second = first.HasValue ? await OptionalDoneRef : Option.None<string>();

        return second.Map(static n => n.Length).ValueOr(-1);
    }
}
