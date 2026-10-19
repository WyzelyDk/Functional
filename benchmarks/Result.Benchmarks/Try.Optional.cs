using BenchmarkDotNet.Attributes;
using Optional;

namespace Wyzely.Benchmarks;

public partial class Try
{
    // Optional has no Try wrapper for Option<T, TException>: the exception is caught and becomes the error, by hand.
    [Benchmark]
    [BenchmarkCategory("ValueSuccess")]
    public int OptionalLib() => RunOptional(static () => Parse("21")).Match(static x => x, static _ => -1);

    [Benchmark]
    [BenchmarkCategory("ValueFailure")]
    public int OptionalFailure() => RunOptional(static () => Parse("x")).Match(static x => x, static _ => -1);

    [Benchmark]
    [BenchmarkCategory("ReferenceSuccess")]
    public int OptionalRef() => RunOptional(static () => Find("ada")).Match(static c => c.Name.Length, static _ => -1);

    [Benchmark]
    [BenchmarkCategory("ReferenceFailure")]
    public int OptionalRefFailure() => RunOptional(static () => Find(string.Empty)).Match(static c => c.Name.Length, static _ => -1);

    private static Option<T, Exception> RunOptional<T>(Func<T> function)
    {
        try
        {
            return Option.Some<T, Exception>(function());
        }
        catch (Exception exception)
        {
            return Option.None<T, Exception>(exception);
        }
    }
}
