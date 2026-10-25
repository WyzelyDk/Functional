using BenchmarkDotNet.Attributes;
using CSharpFunctionalExtensions;

namespace Wyzely.Functional.MaybeBenchmarks;

public partial class Choose
{
    // CSharpFunctionalExtensions has Choose on a sequence of Maybe.
    private readonly CSharpFunctionalExtensions.Maybe<int>[] _csfeNumbers = CsfeMake(Data.Number);
    private readonly CSharpFunctionalExtensions.Maybe<string>[] _csfeNames = CsfeMake(Data.Customer.Name);

    [Benchmark]
    [BenchmarkCategory("Value")]
    public int Csfe() => Total(_csfeNumbers.Choose());

    [Benchmark]
    [BenchmarkCategory("Reference")]
    public int CsfeRef() => Total(_csfeNames.Choose());

    private static CSharpFunctionalExtensions.Maybe<T>[] CsfeMake<T>(T value)
        where T : notnull
    {
        var maybes = new CSharpFunctionalExtensions.Maybe<T>[Count];

        for (var index = 0; index < Count; index++)
        {
            maybes[index] = index % 2 == 0 ? CSharpFunctionalExtensions.Maybe<T>.From(value) : CSharpFunctionalExtensions.Maybe<T>.None;
        }

        return maybes;
    }
}
