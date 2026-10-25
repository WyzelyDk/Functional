using BenchmarkDotNet.Attributes;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Wyzely.Functional.MaybeBenchmarks;

public partial class Choose
{
    // LanguageExt has Somes on a sequence of Option.
    private readonly Option<int>[] _langextNumbers = LangExtMake(Data.Number);
    private readonly Option<string>[] _langextNames = LangExtMake(Data.Customer.Name);

    [Benchmark]
    [BenchmarkCategory("Value")]
    public int LangExt() => Total(_langextNumbers.Somes());

    [Benchmark]
    [BenchmarkCategory("Reference")]
    public int LangExtRef() => Total(_langextNames.Somes());

    private static Option<T>[] LangExtMake<T>(T value)
        where T : notnull
    {
        var maybes = new Option<T>[Count];

        for (var index = 0; index < Count; index++)
        {
            maybes[index] = index % 2 == 0 ? Some(value) : Option<T>.None;
        }

        return maybes;
    }
}
