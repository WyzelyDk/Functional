using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Wyzely.Functional.MaybeBenchmarks;

public partial class Create
{
    [Benchmark]
    [BenchmarkCategory("Value")]
    public int LangExt() => MakeLangExt(_number).Match(static x => x, static () => -1);

    [Benchmark]
    [BenchmarkCategory("Reference")]
    public int LangExtRef() => MakeLangExtRef(_customer).Match(static c => c.Name.Length, static () => -1);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Option<int> MakeLangExt(int value) => Some(value);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Option<Customer> MakeLangExtRef(Customer value) => Some(value);
}
