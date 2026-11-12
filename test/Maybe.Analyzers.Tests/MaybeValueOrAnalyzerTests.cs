using Wyzely.Functional.MaybeAnalyzers;

namespace Wyzely.Functional.MaybeAnalyzersTests;

// The rules about a fallback value: ValueOr instead of a Match that returns the value or a fallback (WYZ0205), the function form of a conditional
// on IsSome (WYZ0203), and a function for a ValueOr whose fallback does work (WYZ0208).
// A rewrite must give exactly the same value, the same type and the same order of evaluation, so a shape that looks alike but is not stays silent.
public sealed class MaybeValueOrAnalyzerTests
{
    private const string Sources = """
        static int Compute() => 42;
        static string Describe() => "described";
        static int Identity(int x) => x;
        static void Log() { }
        static Maybe<int> Find() => Maybe.Some(1);
        static Delegate Build() => (Func<int>)Compute;
        static MulticastDelegate BuildMulticast() => (Func<int>)Compute;
        static Func<int> Maker() => Compute;
        static System.Linq.Expressions.Expression<Func<int>> MakeExpression() => () => 1;
        static Task<object> Later() => Task.FromResult<object>(1);
        sealed record Holder(Maybe<int> Value);
        Maybe<int> Field = Maybe.Some(2);
        const int Limit = 10;
        """;

    private static readonly MaybeSuggestionsAnalyzer Analyzer = new();

    [Theory]
    [InlineData("int M(Maybe<int> m) => m.Match(v => v, () => 0);")]
    [InlineData("string M(Maybe<string> m, string fallback) => m.Match(v => v, () => fallback);")]
    [InlineData("int M(Maybe<int> m) => m.Match(v => v, () => Compute());")]
    [InlineData("int M(Maybe<int> m) => m.Match(v => v, () => { return Compute(); });")]
    [InlineData("int M(Maybe<int> m) => m.Match(v => { return v; }, () => 0);")]
    [InlineData("int M(Maybe<int> m) => m.Match(static v => v, static () => 0);")]
    [InlineData("int M(Maybe<int> m) => m.Match(onSome: v => v, onNone: () => 0);")]
    [InlineData("int M(Maybe<int> m) => m.Match(onNone: () => 0, onSome: v => v);")]
    [InlineData("List<int> M(Maybe<List<int>> m) => m.Match(v => v, () => new List<int>());")]
    [InlineData("int M() => Find().Match(v => v, () => 0);")]
    [InlineData("int M(Holder h) => h.Value.Match(v => v, () => 0);")]
    [InlineData("Task<int> M(Task<Maybe<int>> m) => m.Match(v => v, () => 0);")]
    [InlineData("Task<int> M(Task<Maybe<int>> m) => m.Match(v => v, () => Compute());")]
    [InlineData("ValueTask<int> M(ValueTask<Maybe<int>> m) => m.Match(v => v, () => 0);")]
    [InlineData("ValueTask<int> M(ValueTask<Maybe<int>> m) => m.Match(v => v, () => Compute());")]
    [InlineData("object M(Maybe<object> m, object fallback) => m.Match(v => v, () => fallback);")]
    [InlineData("Func<int> M(Maybe<Func<int>> m, Func<int> fallback) => m.Match(v => v, () => fallback);")]
    [InlineData("Task<object> M(Task<Maybe<object>> m, object fallback) => m.Match(v => v, () => fallback);")]
    [InlineData("Task<int> M(Task<Maybe<int>> m) => m.Match(v => v, () => default(int));")]
    [InlineData("Task<Func<int>> M(Task<Maybe<Func<int>>> m) => m.Match(v => v, () => Maker());")]
    internal async Task Match_that_returns_the_value_or_a_fallback_is_ValueOr(string method)
    {
        var ids = await AnalyzerTestHost.AnalyzeAsync(Sources + "\n" + method, Analyzer);

        Assert.Equal(["WYZ0205"], ids);
    }

    [Theory]
    [InlineData("int M(Maybe<int> m) => m.Match(v => v + 1, () => 0);")]
    [InlineData("object M(Maybe<int> m) => m.Match<int, object>(v => v, () => 0);")]
    [InlineData("object M(Maybe<int> m) => m.Match(v => (object)v, () => 0);")]
    [InlineData("int M(Maybe<int> m) => m.Match(v => 1, () => 0);")]
    [InlineData("int M(Maybe<int> m) => m.Match(v => v, Compute);")]
    [InlineData("int M(Maybe<int> m) => m.Match(Identity, () => 0);")]
    [InlineData("int M(Maybe<int> m) => m.Match(v => v, () => { Log(); return 0; });")]
    [InlineData("int M(Maybe<int> m) => m.Match(v => { Log(); return v; }, () => 0);")]
    [InlineData("int M(Maybe<int> m) => m.Match(v => v, () => throw new InvalidOperationException());")]
    [InlineData("List<int> M(Maybe<List<int>> m) => m.Match(v => v, () => new());")]
    [InlineData("Task<List<int>> M(Task<Maybe<List<int>>> m) => m.Match(v => v, () => new());")]
    [InlineData("Task<string> M(Task<Maybe<string>> m) => m.Match(v => v, () => null!);")]
    [InlineData("Task<int> M(Task<Maybe<int>> m) => m.Match(v => v, () => default);")]
    [InlineData("ValueTask<string> M(ValueTask<Maybe<string>> m) => m.Match(v => v, () => null);")]
    [InlineData("Task<int> M(Maybe<int> m) => m.Match(async v => v, async () => 0);")]
    [InlineData("Task<int> M(Maybe<int> m) => m.Match(v => Task.FromResult(v), () => Task.FromResult(0));")]
    [InlineData("Task<int> M(Task<Maybe<int>> m) => m.Match(async v => v, () => Task.FromResult(0));")]
    [InlineData("int M(Maybe<int> m) => MaybeExtensions.Match(m, v => v, () => 0);")]
    [InlineData("int M(Maybe<int> m) => m.Match<int, int>(v => v, () => 0);")]
    [InlineData("int M(Maybe<int> m) => m.ValueOr(() => 0);")]
    [InlineData("object M(Maybe<object> m) => m.Match(v => v, () => Compute());")]
    [InlineData("Delegate M(Maybe<Delegate> m) => m.Match(v => v, () => Build());")]
    [InlineData("MulticastDelegate M(Maybe<MulticastDelegate> m) => m.Match(v => v, () => BuildMulticast());")]
    [InlineData("Func<int> M(Maybe<Func<int>> m) => m.Match(v => v, () => Maker());")]
    [InlineData("dynamic M(Maybe<dynamic> m) => m.Match(v => v, () => Compute());")]
    [InlineData(
        "System.Linq.Expressions.Expression<Func<int>> M(Maybe<System.Linq.Expressions.Expression<Func<int>>> m) =>"
        + " m.Match(v => v, () => MakeExpression());")]
    [InlineData("Task<object> M(Task<Maybe<object>> m) => m.Match(v => v, () => Later());")]
    [InlineData("ValueTask<dynamic> M(ValueTask<Maybe<dynamic>> m) => m.Match(v => v, () => Compute());")]
    [InlineData("object M(Maybe<object> m) => m.Match(v => v, () => Later());")]
    internal async Task Match_that_ValueOr_would_not_say_the_same_is_left_alone(string method)
    {
        var ids = await AnalyzerTestHost.AnalyzeAsync(Sources + "\n" + method, Analyzer);

        Assert.DoesNotContain("WYZ0205", ids);
    }

    // The fallback is cheap: ValueOr(fallback). Not cheap: ValueOr(() => fallback), evaluated only when there is no value.
    [Theory]
    [InlineData("int M(Maybe<int> m) => m.IsSome ? m.GetValue() : Compute();")]
    [InlineData("int M(Maybe<int> m) => m.IsNone ? Compute() : m.GetValue();")]
    [InlineData("int M(Maybe<int> m) => m.IsSome ? m.GetValue() : (Compute());")]
    [InlineData("string M(Maybe<string> m) => m.IsSome ? m.GetValue() : Describe();")]
    [InlineData("List<int> M(Maybe<List<int>> m) => m.IsSome ? m.GetValue() : new List<int>();")]
    [InlineData("int[] M(Maybe<int[]> m) => m.IsSome ? m.GetValue() : new int[0];")]
    [InlineData("int M(Maybe<int> m, int add) => m.IsSome ? m.GetValue() : Compute() + add;")]
    [InlineData("string M(Maybe<string> m, int id) => m.IsSome ? m.GetValue() : $\"unknown {id}\";")]
    [InlineData("int M() => Field.IsSome ? Field.GetValue() : Compute();")]
    [InlineData("int M(Maybe<int> m, Maybe<int> other) => m.IsSome ? m.GetValue() : other.GetValue();")]
    [InlineData("object M(Maybe<object> m, object fallback) => m.IsSome ? m.GetValue() : fallback;")]
    [InlineData("Func<int> M(Maybe<Func<int>> m, Func<int> fallback) => m.IsSome ? m.GetValue() : fallback;")]
    [InlineData("Task<object> M(Maybe<Task<object>> m) => m.IsSome ? m.GetValue() : Later();")]
    internal async Task A_conditional_on_IsSome_is_ValueOr_whatever_the_fallback_costs(string method)
    {
        var ids = await AnalyzerTestHost.AnalyzeAsync(Sources + "\n" + method, Analyzer);

        Assert.Equal(["WYZ0203"], ids);
    }

    // Each of these looks like the conditional above and would change the behavior or the type, or does not compile when it is rewritten.
    [Theory]
    [InlineData("double M(Maybe<int> m) => m.IsSome ? m.GetValue() : 1.5;")]
    [InlineData("double M(Maybe<int> m) => m.IsSome ? m.GetValue() : Compute() + 0.5;")]
    [InlineData("int M(Maybe<int> m) => m.IsSome ? m.GetValue() : throw new InvalidOperationException();")]
    [InlineData("int M(Maybe<int> m, string s) => m.IsSome ? m.GetValue() : (int.TryParse(s, out var n) ? n : 0);")]
    [InlineData("int M(Maybe<int> m, string s) { int n; return m.IsSome ? m.GetValue() : (int.TryParse(s, out n) ? n : 0); }")]
    [InlineData("int M(Maybe<int> m, object o) => m.IsSome ? m.GetValue() : (o is int n ? n : Compute());")]
    [InlineData("async Task<int> M(Maybe<int> m) => m.IsSome ? m.GetValue() : await Task.FromResult(1);")]
    [InlineData("int M(Maybe<int> m, Span<int> span) => m.IsSome ? m.GetValue() : span.Length + Compute();")]
    [InlineData("int M(Maybe<int> m) { Span<int> span = stackalloc int[1]; return m.IsSome ? m.GetValue() : span.Length + Compute(); }")]
    [InlineData("int M(Maybe<int> m, in int x) => m.IsSome ? m.GetValue() : x + Compute();")]
    [InlineData("int M(Maybe<int> m, int[] a) { ref int x = ref a[0]; return m.IsSome ? m.GetValue() : x + Compute(); }")]
    [InlineData("List<int> M(Maybe<List<int>> m) => m.IsSome ? m.GetValue() : new();")]
    [InlineData("object M(Maybe<object> m) => m.IsSome ? m.GetValue() : Compute();")]
    [InlineData("Delegate M(Maybe<Delegate> m) => m.IsSome ? m.GetValue() : Build();")]
    [InlineData("MulticastDelegate M(Maybe<MulticastDelegate> m) => m.IsSome ? m.GetValue() : BuildMulticast();")]
    [InlineData("Func<int> M(Maybe<Func<int>> m) => m.IsSome ? m.GetValue() : Maker();")]
    [InlineData("dynamic M(Maybe<dynamic> m) => m.IsSome ? m.GetValue() : Compute();")]
    [InlineData(
        "System.Linq.Expressions.Expression<Func<int>> M(Maybe<System.Linq.Expressions.Expression<Func<int>>> m) =>"
        + " m.IsSome ? m.GetValue() : MakeExpression();")]
    [InlineData("object M(Maybe<object> m) => m.IsSome ? m.GetValue() : Later();")]
    internal async Task A_conditional_that_ValueOr_would_not_say_the_same_is_left_alone(string method)
    {
        var ids = await AnalyzerTestHost.AnalyzeAsync(Sources + "\n" + method, Analyzer);

        Assert.DoesNotContain("WYZ0203", ids);
    }

    [Fact]
    internal async Task A_conditional_in_a_struct_whose_lazy_fallback_reads_the_struct_is_left_alone_because_a_lambda_cannot_capture_it()
    {
        const string source = """
            public struct Counter
            {
                public int Count;
                public Maybe<int> Value;
                static int Compute() => 1;
                public int Lazy() => Value.IsSome ? Value.GetValue() : Count + Compute();
                public int Cheap() => Value.IsSome ? Value.GetValue() : Count;
            }
            """;

        var ids = await AnalyzerTestHost.AnalyzeSourceAsync(source, Analyzer);

        Assert.Equal(["WYZ0203"], ids);
    }

    [Theory]
    [InlineData("int M(Maybe<int> m) => m.ValueOr(Compute());")]
    [InlineData("string M(Maybe<string> m) => m.ValueOr(Describe());")]
    [InlineData("List<int> M(Maybe<List<int>> m) => m.ValueOr(new List<int>());")]
    [InlineData("int[] M(Maybe<int[]> m) => m.ValueOr(new int[0]);")]
    [InlineData("int[] M(Maybe<int[]> m) => m.ValueOr(new[] { 1, 2 });")]
    [InlineData("Task<int> M(Task<Maybe<int>> m) => m.ValueOr(Compute());")]
    [InlineData("ValueTask<int> M(ValueTask<Maybe<int>> m) => m.ValueOr(Compute());")]
    [InlineData("Task<List<int>> M(Task<Maybe<List<int>>> m) => m.ValueOr(new List<int>());")]
    [InlineData("List<int> M(Maybe<List<int>> m) => m.ValueOr([1, 2]);")]
    [InlineData("string M(Maybe<string> m, int id) => m.ValueOr($\"unknown {id}\");")]
    [InlineData("DateTime M(Maybe<DateTime> m) => m.ValueOr(new DateTime(2020, 1, 1));")]
    [InlineData("int M(Maybe<int> m) => m.ValueOr(Compute() + 1);")]
    [InlineData("int M(Maybe<int> m) => m.ValueOr(fallback: Compute());")]
    [InlineData("int M(Maybe<int> m) => m.ValueOr((Compute()));")]
    [InlineData("int M(Holder h) => h.Value.ValueOr(Compute());")]
    [InlineData("int M() => Find().ValueOr(Compute());")]
    [InlineData("int M() => Field.ValueOr(Compute());")]
    [InlineData("int M(Maybe<int> m, Maybe<int> other) => m.ValueOr(other.GetValue());")]
    internal async Task ValueOr_with_a_fallback_that_does_work_takes_a_function_instead(string method)
    {
        var ids = await AnalyzerTestHost.AnalyzeAsync(Sources + "\n" + method, Analyzer);

        Assert.Equal(["WYZ0208"], ids);
    }

    // Reading a local, a parameter, a field, a property or a constant is not work, and a function, or a value that a lambda would become, is left alone.
    [Theory]
    [InlineData("int M(Maybe<int> m) => m.ValueOr(0);")]
    [InlineData("int M(Maybe<int> m) => m.ValueOr(Limit);")]
    [InlineData("int M(Maybe<int> m) => m.ValueOr(int.MaxValue);")]
    [InlineData("int M(Maybe<int> m) => m.ValueOr(default);")]
    [InlineData("int M(Maybe<int> m, int fallback) => m.ValueOr(fallback);")]
    [InlineData("int M(Maybe<int> m, List<int> list) => m.ValueOr(list.Count);")]
    [InlineData("string M(Maybe<string> m) => m.ValueOr(\"none\");")]
    [InlineData("string M(Maybe<string> m) => m.ValueOr($\"none\");")]
    [InlineData("int M(Maybe<int> m) => m.ValueOr(Field.IsSome ? 1 : 2);")]
    [InlineData("int M(Maybe<int> m) => m.ValueOr(() => Compute());")]
    [InlineData("List<int> M(Maybe<List<int>> m) => m.ValueOr(new());")]
    [InlineData("Task<int> M(Task<Maybe<int>> m) => m.ValueOr(() => Compute());")]
    [InlineData("Task<int> M(Task<Maybe<int>> m) => m.ValueOr(0);")]
    [InlineData("Task<object> M(Task<Maybe<object>> m) => m.ValueOr(Compute());")]
    [InlineData("ValueTask<int> M(ValueTask<Maybe<int>> m) => m.ValueOr(() => Compute());")]
    [InlineData("int? M(Maybe<int>? m) => m?.ValueOr(Compute());")]
    [InlineData("async Task<int> M(Maybe<int> m) => m.ValueOr(await Task.FromResult(1));")]
    [InlineData("int M(Maybe<int> m, string s) => m.ValueOr(int.TryParse(s, out var n) ? n : Compute());")]
    [InlineData("int M(Maybe<int> m, string s) { int n; return m.ValueOr(int.TryParse(s, out n) ? n : Compute()); }")]
    [InlineData("int M(Maybe<int> m, object o) => m.ValueOr(o is int n ? n : Compute());")]
    [InlineData("int M(Maybe<int> m, Span<int> span) => m.ValueOr(span.Length + Compute());")]
    [InlineData("int M(Maybe<int> m) { Span<int> span = stackalloc int[1]; return m.ValueOr(span.Length + Compute()); }")]
    [InlineData("int M(Maybe<int> m, in int x) => m.ValueOr(x + Compute());")]
    [InlineData("int M(Maybe<int> m, int[] a) { ref int x = ref a[0]; return m.ValueOr(x + Compute()); }")]
    [InlineData("object M(Maybe<object> m) => m.ValueOr(Compute());")]
    [InlineData("Delegate M(Maybe<Delegate> m) => m.ValueOr(Build());")]
    [InlineData("MulticastDelegate M(Maybe<MulticastDelegate> m) => m.ValueOr(BuildMulticast());")]
    [InlineData("Func<int> M(Maybe<Func<int>> m) => m.ValueOr(Maker());")]
    [InlineData("dynamic M(Maybe<dynamic> m) => m.ValueOr(Compute());")]
    [InlineData(
        "System.Linq.Expressions.Expression<Func<int>> M(Maybe<System.Linq.Expressions.Expression<Func<int>>> m) =>"
        + " m.ValueOr(MakeExpression());")]
    [InlineData("object M(Maybe<object> m) => m.ValueOr(Later());")]
    internal async Task ValueOr_that_needs_no_function_or_cannot_take_one_is_left_alone(string method)
    {
        var ids = await AnalyzerTestHost.AnalyzeAsync(Sources + "\n" + method, Analyzer);

        Assert.DoesNotContain("WYZ0208", ids);
    }

    [Fact]
    internal async Task A_ValueOr_in_a_struct_whose_fallback_reads_the_struct_is_left_alone_because_a_lambda_cannot_capture_it()
    {
        const string source = """
            public struct Counter
            {
                public int Count;
                public Maybe<int> Value;
                static int Compute() => 1;
                public int Reads() => Value.ValueOr(Count + Compute());
                public int Calls() => Value.ValueOr(Compute());
            }
            """;

        var ids = await AnalyzerTestHost.AnalyzeSourceAsync(source, Analyzer);

        Assert.Equal(["WYZ0208"], ids);
    }

    [Fact]
    internal async Task Look_alikes_of_Maybe_and_of_Result_are_left_alone()
    {
        const string source = """
            namespace Other
            {
                public readonly struct Maybe<T>
                {
                    public bool IsSome => true;
                    public T GetValue() => default!;
                    public T ValueOr(T fallback) => fallback;
                    public TResult Match<TResult>(Func<T, TResult> onSome, Func<TResult> onNone) => onNone();
                }

                public sealed class Result<T>
                {
                    public bool IsSuccess => true;
                    public T GetValue() => default!;
                    public T ValueOr(T fallback) => fallback;
                    public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<string, TResult> onFailure) => onFailure("x");
                }

                public static class Use
                {
                    static int Compute() => 1;
                    public static int Function(Maybe<int> m) => m.ValueOr(Compute());
                    public static int Conditional(Maybe<int> m) => m.IsSome ? m.GetValue() : Compute();
                    public static int Matched(Maybe<int> m) => m.Match(v => v, () => Compute());
                    public static int ResultFunction(Result<int> r) => r.ValueOr(Compute());
                    public static int ResultConditional(Result<int> r) => r.IsSuccess ? r.GetValue() : Compute();
                    public static int ResultMatched(Result<int> r) => r.Match(v => v, e => Compute());
                }
            }
            """;

        var ids = await AnalyzerTestHost.AnalyzeSourceAsync(source, Analyzer);

        Assert.Empty(ids);
    }
}
