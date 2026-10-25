using Ardalis.Result;
using BenchmarkDotNet.Attributes;

namespace Wyzely.Benchmarks;

public partial class Combine
{
    // Ardalis.Result has no combine method: the loop collects the values and returns at the first failure. Its Result<T> is a class.
    private readonly Result<int>[] _ardalisNumbers = ArdalisMake(Data.Number, fail: false);
    private readonly Result<int>[] _ardalisNumbersFailed = ArdalisMake(Data.Number, fail: true);
    private readonly Result<string>[] _ardalisNames = ArdalisMake(Data.Customer.Name, fail: false);
    private readonly Result<string>[] _ardalisNamesFailed = ArdalisMake(Data.Customer.Name, fail: true);

    [Benchmark]
    [BenchmarkCategory("ValueSuccess")]
    public int Ardalis() => ArdalisCombine(_ardalisNumbers);

    [Benchmark]
    [BenchmarkCategory("ValueFailure")]
    public int ArdalisFailure() => ArdalisCombine(_ardalisNumbersFailed);

    [Benchmark]
    [BenchmarkCategory("ReferenceSuccess")]
    public int ArdalisRef() => ArdalisCombine(_ardalisNames);

    [Benchmark]
    [BenchmarkCategory("ReferenceFailure")]
    public int ArdalisRefFailure() => ArdalisCombine(_ardalisNamesFailed);

    private static Result<T>[] ArdalisMake<T>(T value, bool fail)
    {
        var results = new Result<T>[Count];

        for (var index = 0; index < Count; index++)
        {
            results[index] = fail && index == FailingIndex ? Result<T>.Error("Rejected.") : Result<T>.Success(value);
        }

        return results;
    }

    private static int ArdalisCombine<T>(Result<T>[] results)
    {
        var values = new List<T>(Count);

        foreach (var result in results)
        {
            if (!result.IsSuccess)
            {
                return -1;
            }

            values.Add(result.Value);
        }

        return values.Count;
    }
}
