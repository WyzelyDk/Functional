using BenchmarkDotNet.Attributes;
using FluentResults;

namespace Wyzely.Benchmarks;

public partial class Combine
{
    // Result.Merge takes the results and gives a result with the list of the values, and the reasons of all the failures.
    private readonly Result<int>[] _fluentNumbers = FluentMake(Data.Number, fail: false);
    private readonly Result<int>[] _fluentNumbersFailed = FluentMake(Data.Number, fail: true);
    private readonly Result<string>[] _fluentNames = FluentMake(Data.Customer.Name, fail: false);
    private readonly Result<string>[] _fluentNamesFailed = FluentMake(Data.Customer.Name, fail: true);

    [Benchmark]
    [BenchmarkCategory("ValueSuccess")]
    public int Fluent() => FluentCount(Result.Merge(_fluentNumbers));

    [Benchmark]
    [BenchmarkCategory("ValueFailure")]
    public int FluentFailure() => FluentCount(Result.Merge(_fluentNumbersFailed));

    [Benchmark]
    [BenchmarkCategory("ReferenceSuccess")]
    public int FluentRef() => FluentCount(Result.Merge(_fluentNames));

    [Benchmark]
    [BenchmarkCategory("ReferenceFailure")]
    public int FluentRefFailure() => FluentCount(Result.Merge(_fluentNamesFailed));

    private static Result<T>[] FluentMake<T>(T value, bool fail)
    {
        var results = new Result<T>[Count];

        for (var index = 0; index < Count; index++)
        {
            results[index] = fail && index == FailingIndex ? Result.Fail<T>("Rejected.") : Result.Ok(value);
        }

        return results;
    }

    private static int FluentCount<T>(Result<IEnumerable<T>> merged) => merged.IsSuccess ? merged.Value.Count() : -1;
}
