using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Wyzely.Benchmarks;

public partial class Create
{
    [Benchmark]
    [BenchmarkCategory("Value")]
    public int LangExt() => MakeLangExt(_number).Match(static x => x, static _ => -1);

    [Benchmark]
    [BenchmarkCategory("Reference")]
    public int LangExtRef() => MakeLangExtRef(_customer).Match(static c => c.Name.Length, static _ => -1);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Fin<int> MakeLangExt(int value) => FinSucc(value);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Fin<Customer> MakeLangExtRef(Customer value) => FinSucc(value);
}
