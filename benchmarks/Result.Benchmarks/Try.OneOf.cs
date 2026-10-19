using BenchmarkDotNet.Attributes;
using OneOf;
using OneOf.Types;

namespace Wyzely.Benchmarks;

public partial class Try
{
    // OneOf has no Try wrapper: the exception is caught and turned into an Error<string> by hand.
    [Benchmark]
    [BenchmarkCategory("ValueSuccess")]
    public int OneOfLib() => RunOneOf(static () => Parse("21")).Match(static x => x, static _ => -1);

    [Benchmark]
    [BenchmarkCategory("ValueFailure")]
    public int OneOfFailure() => RunOneOf(static () => Parse("x")).Match(static x => x, static _ => -1);

    [Benchmark]
    [BenchmarkCategory("ReferenceSuccess")]
    public int OneOfRef() => RunOneOf(static () => Find("ada")).Match(static c => c.Name.Length, static _ => -1);

    [Benchmark]
    [BenchmarkCategory("ReferenceFailure")]
    public int OneOfRefFailure() => RunOneOf(static () => Find(string.Empty)).Match(static c => c.Name.Length, static _ => -1);

    private static OneOf<T, Error<string>> RunOneOf<T>(Func<T> function)
    {
        try
        {
            return function();
        }
        catch (Exception exception)
        {
            return new Error<string>(exception.Message);
        }
    }
}
