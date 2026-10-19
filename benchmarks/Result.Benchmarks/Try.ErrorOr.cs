using BenchmarkDotNet.Attributes;
using ErrorOr;

namespace Wyzely.Benchmarks;

public partial class Try
{
    // ErrorOr has no Try wrapper: the exception is caught and turned into an error by hand.
    [Benchmark]
    [BenchmarkCategory("ValueSuccess")]
    public int ErrorOrLib() => RunErrorOr(static () => Parse("21")).Match(static x => x, static _ => -1);

    [Benchmark]
    [BenchmarkCategory("ValueFailure")]
    public int ErrorOrFailure() => RunErrorOr(static () => Parse("x")).Match(static x => x, static _ => -1);

    [Benchmark]
    [BenchmarkCategory("ReferenceSuccess")]
    public int ErrorOrRef() => RunErrorOr(static () => Find("ada")).Match(static c => c.Name.Length, static _ => -1);

    [Benchmark]
    [BenchmarkCategory("ReferenceFailure")]
    public int ErrorOrRefFailure() => RunErrorOr(static () => Find(string.Empty)).Match(static c => c.Name.Length, static _ => -1);

    private static ErrorOr<T> RunErrorOr<T>(Func<T> function)
    {
        try
        {
            return function();
        }
        catch (Exception exception)
        {
            return Error.Failure("Exception", exception.Message);
        }
    }
}
