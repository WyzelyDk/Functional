using BenchmarkDotNet.Attributes;
using Optional;

namespace Wyzely.Benchmarks;

public partial class Combine
{
    // Option<T, TException> has no combine method: the loop collects the values and returns at the first failure.
    private readonly Option<int, string>[] _optionalNumbers = OptionalMake(Data.Number, fail: false);
    private readonly Option<int, string>[] _optionalNumbersFailed = OptionalMake(Data.Number, fail: true);
    private readonly Option<string, string>[] _optionalNames = OptionalMake(Data.Customer.Name, fail: false);
    private readonly Option<string, string>[] _optionalNamesFailed = OptionalMake(Data.Customer.Name, fail: true);

    [Benchmark]
    [BenchmarkCategory("ValueSuccess")]
    public int OptionalLib() => OptionalCombine(_optionalNumbers);

    [Benchmark]
    [BenchmarkCategory("ValueFailure")]
    public int OptionalLibFailure() => OptionalCombine(_optionalNumbersFailed);

    [Benchmark]
    [BenchmarkCategory("ReferenceSuccess")]
    public int OptionalLibRef() => OptionalCombine(_optionalNames);

    [Benchmark]
    [BenchmarkCategory("ReferenceFailure")]
    public int OptionalLibRefFailure() => OptionalCombine(_optionalNamesFailed);

    private static Option<T, string>[] OptionalMake<T>(T value, bool fail)
    {
        var results = new Option<T, string>[Count];

        for (var index = 0; index < Count; index++)
        {
            results[index] = fail && index == FailingIndex ? Option.None<T, string>("Rejected.") : Option.Some<T, string>(value);
        }

        return results;
    }

    private static int OptionalCombine<T>(Option<T, string>[] results)
    {
        var values = new List<T>(Count);

        foreach (var result in results)
        {
            if (!result.HasValue)
            {
                return -1;
            }

            values.Add(result.ValueOr(default(T)!));
        }

        return values.Count;
    }
}
