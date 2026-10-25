using BenchmarkDotNet.Attributes;
using ErrorOr;

namespace Wyzely.Benchmarks;

public partial class Combine
{
    // ErrorOr has no combine method: the loop collects the values and returns at the first failure.
    private readonly ErrorOr<int>[] _errorOrNumbers = ErrorOrMake(Data.Number, fail: false);
    private readonly ErrorOr<int>[] _errorOrNumbersFailed = ErrorOrMake(Data.Number, fail: true);
    private readonly ErrorOr<string>[] _errorOrNames = ErrorOrMake(Data.Customer.Name, fail: false);
    private readonly ErrorOr<string>[] _errorOrNamesFailed = ErrorOrMake(Data.Customer.Name, fail: true);

    [Benchmark]
    [BenchmarkCategory("ValueSuccess")]
    public int ErrorOr() => ErrorOrCombine(_errorOrNumbers);

    [Benchmark]
    [BenchmarkCategory("ValueFailure")]
    public int ErrorOrFailure() => ErrorOrCombine(_errorOrNumbersFailed);

    [Benchmark]
    [BenchmarkCategory("ReferenceSuccess")]
    public int ErrorOrRef() => ErrorOrCombine(_errorOrNames);

    [Benchmark]
    [BenchmarkCategory("ReferenceFailure")]
    public int ErrorOrRefFailure() => ErrorOrCombine(_errorOrNamesFailed);

    private static ErrorOr<T>[] ErrorOrMake<T>(T value, bool fail)
    {
        var results = new ErrorOr<T>[Count];

        for (var index = 0; index < Count; index++)
        {
            results[index] = fail && index == FailingIndex ? Error.Failure("Rejected", "Rejected.") : value;
        }

        return results;
    }

    private static int ErrorOrCombine<T>(ErrorOr<T>[] results)
    {
        var values = new List<T>(Count);

        foreach (var result in results)
        {
            if (result.IsError)
            {
                return -1;
            }

            values.Add(result.Value);
        }

        return values.Count;
    }
}
