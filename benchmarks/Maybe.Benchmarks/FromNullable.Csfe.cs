using BenchmarkDotNet.Attributes;
using CSharpFunctionalExtensions;
using CsfeString = CSharpFunctionalExtensions.Maybe<string>;

namespace Wyzely.Functional.MaybeBenchmarks;

public partial class FromNullable
{
    // AsMaybe takes a nullable by reference, so the field is copied to a local first.
    [Benchmark]
    [BenchmarkCategory("ValuePresent")]
    public int Csfe()
    {
        var value = _present;

        return value.AsMaybe().Match(static x => x, static () => -1);
    }

    [Benchmark]
    [BenchmarkCategory("ValueAbsent")]
    public int CsfeAbsent()
    {
        var value = _absent;

        return value.AsMaybe().Match(static x => x, static () => -1);
    }

    [Benchmark]
    [BenchmarkCategory("ReferencePresent")]
    public int CsfeRef() => CsfeString.From(_presentText!).Match(static s => s.Length, static () => -1);

    [Benchmark]
    [BenchmarkCategory("ReferenceAbsent")]
    public int CsfeRefAbsent() => CsfeString.From(_absentText!).Match(static s => s.Length, static () => -1);
}
