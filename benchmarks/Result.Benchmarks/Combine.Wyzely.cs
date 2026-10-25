using BenchmarkDotNet.Attributes;
using Wyzely.Functional;

namespace Wyzely.Benchmarks;

public partial class Combine
{
    private static readonly Error WyzelyRejected = Error.Create("Rejected.");

    private readonly Result<int>[] _wyzelyNumbers = WyzelyMake(Data.Number, fail: false);
    private readonly Result<int>[] _wyzelyNumbersFailed = WyzelyMake(Data.Number, fail: true);
    private readonly Result<string>[] _wyzelyNames = WyzelyMake(Data.Customer.Name, fail: false);
    private readonly Result<string>[] _wyzelyNamesFailed = WyzelyMake(Data.Customer.Name, fail: true);

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("ValueSuccess")]
    public int Wyzely() => _wyzelyNumbers.Combine().Match(static x => x.Count, static _ => -1);

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("ValueFailure")]
    public int WyzelyFailure() => _wyzelyNumbersFailed.Combine().Match(static x => x.Count, static _ => -1);

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("ReferenceSuccess")]
    public int WyzelyRef() => _wyzelyNames.Combine().Match(static x => x.Count, static _ => -1);

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("ReferenceFailure")]
    public int WyzelyRefFailure() => _wyzelyNamesFailed.Combine().Match(static x => x.Count, static _ => -1);

    private static Result<T>[] WyzelyMake<T>(T value, bool fail)
    {
        var results = new Result<T>[Count];

        for (var index = 0; index < Count; index++)
        {
            results[index] = fail && index == FailingIndex ? Result.Failure<T>(WyzelyRejected) : Result.Success(value);
        }

        return results;
    }
}
