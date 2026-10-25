using BenchmarkDotNet.Attributes;
using LanguageExt;
using LanguageExt.Common;
using static LanguageExt.Prelude;

namespace Wyzely.Benchmarks;

public partial class Combine
{
    // Fin<A> is combined with Sequence, which turns a sequence of Fin<A> into a Fin of the sequence of A.
    private static readonly Error LangExtRejected = Error.New("Rejected.");

    private readonly Fin<int>[] _langExtNumbers = LangExtMake(Data.Number, fail: false);
    private readonly Fin<int>[] _langExtNumbersFailed = LangExtMake(Data.Number, fail: true);
    private readonly Fin<string>[] _langExtNames = LangExtMake(Data.Customer.Name, fail: false);
    private readonly Fin<string>[] _langExtNamesFailed = LangExtMake(Data.Customer.Name, fail: true);

    [Benchmark]
    [BenchmarkCategory("ValueSuccess")]
    public int LangExt() => LangExtCount(_langExtNumbers);

    [Benchmark]
    [BenchmarkCategory("ValueFailure")]
    public int LangExtFailure() => LangExtCount(_langExtNumbersFailed);

    [Benchmark]
    [BenchmarkCategory("ReferenceSuccess")]
    public int LangExtRef() => LangExtCount(_langExtNames);

    [Benchmark]
    [BenchmarkCategory("ReferenceFailure")]
    public int LangExtRefFailure() => LangExtCount(_langExtNamesFailed);

    private static Fin<T>[] LangExtMake<T>(T value, bool fail)
    {
        var results = new Fin<T>[Count];

        for (var index = 0; index < Count; index++)
        {
            results[index] = fail && index == FailingIndex ? FinFail<T>(LangExtRejected) : FinSucc(value);
        }

        return results;
    }

    private static int LangExtCount<T>(Fin<T>[] results) => results.AsEnumerable().Sequence().Match(static x => x.Count(), static _ => -1);
}
