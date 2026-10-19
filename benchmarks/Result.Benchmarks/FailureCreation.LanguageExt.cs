using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using LanguageExt;
using LanguageExt.Common;
using static LanguageExt.Prelude;

namespace Wyzely.Benchmarks;

public partial class FailureCreation
{
    [Benchmark]
    [BenchmarkCategory("Value")]
    public bool LangExt() => MakeLangExt().IsFail;

    [Benchmark]
    [BenchmarkCategory("Reference")]
    public bool LangExtRef() => MakeLangExtRef().IsFail;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Fin<int> MakeLangExt() => FinFail<int>(Error.New("Rejected."));

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Fin<Customer> MakeLangExtRef() => FinFail<Customer>(Error.New("Rejected."));
}
