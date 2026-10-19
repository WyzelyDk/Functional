using BenchmarkDotNet.Attributes;
using Optional;

namespace Wyzely.Benchmarks;

public partial class SyncToAsync
{
    private static readonly Task<Option<int, string>> OptionalDone = Task.FromResult(Option.Some<int, string>(43));
    private static readonly Task<Option<string, string>> OptionalDoneRef = Task.FromResult(Option.Some<string, string>(Data.Customer.Name));

    [Benchmark]
    [BenchmarkCategory("Value")]
    public async ValueTask<int> OptionalLib()
    {
        var first = Option.Some<int, string>(_number);
        var second = first.HasValue ? await OptionalDone : Option.None<int, string>("Rejected.");

        return second.Map(static x => x + 1).ValueOr(-1);
    }

    [Benchmark]
    [BenchmarkCategory("Reference")]
    public async ValueTask<int> OptionalRef()
    {
        var first = Option.Some<Customer, string>(_customer);
        var second = first.HasValue ? await OptionalDoneRef : Option.None<string, string>("Rejected.");

        return second.Map(static n => n.Length).ValueOr(-1);
    }
}
