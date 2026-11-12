using Wyzely.Functional.ResultAnalyzers;

namespace Wyzely.Functional.ResultAnalyzersTests;

// The rules about the error and the fallback of a result: MapError instead of an OrElse that only returns an error (WYZ0118),
// ValueOr instead of a conditional on the state of a result (WYZ0119), and a function for a ValueOr whose fallback does work (WYZ0120).
// A rewrite must give exactly the same result, the same type and the same order of evaluation, so a shape that looks alike but is not stays silent.
public sealed class FallbackAnalyzerTests
{
    private const string Sources = """
        static int Compute() => 42;
        static string Describe() => "described";
        static int Apply(Func<int, int> function) => function(1);
        static Result<int> Make() => Result.Success(1);
        static Result<int> Fail(Error e) => e;
        static int GetValue(Result<int> r) => 1;
        static void Log(Error e) { }
        static readonly Error Rejected = Error.Create("Rejected.");
        static Error Failed() => Error.Create("Failed.");
        static Delegate Build() => (Func<int>)Compute;
        static MulticastDelegate BuildMulticast() => (Func<int>)Compute;
        static Func<int> Maker() => Compute;
        static System.Linq.Expressions.Expression<Func<int>> MakeExpression() => () => 1;
        sealed record Holder(Result<int> Value);
        sealed class Switch { public bool IsOn => true; }
        Result<int> Field = Result.Success(2);
        """;

    private static readonly ResultSuggestionsAnalyzer Analyzer = new();

    [Theory]
    [InlineData("Result<int> M(Result<int> r) => r.OrElse(e => Rejected);")]
    [InlineData("Result<int> M(Result<int> r) => r.OrElse(() => Rejected);")]
    [InlineData("Result<int> M(Result<int> r) => r.OrElse(e => e.WithCode(\"A\"));")]
    [InlineData("Result<int> M(Result<int> r) => r.OrElse(static e => Rejected);")]
    [InlineData("Result<int> M(Result<int> r) => r.OrElse((Error e) => Failed());")]
    [InlineData("Result<int> M(Result<int> r) => r.OrElse(Result<int> () => Rejected);")]
    [InlineData("Result<int> M(Result<int> r) => r.OrElse(Result<int> (Error e) => Rejected);")]
    [InlineData("Result<int> M(Result<int> r) => r.OrElse(e => { return Rejected; });")]
    [InlineData("Result<int> M(Result<int> r) => r.OrElse(e => e.Kind == ErrorKind.NotFound ? Rejected : e);")]
    [InlineData("Result<int> M(Result<int> r) => r.OrElse(e => Result.Failure<int>(Rejected));")]
    [InlineData("Result<int> M(Result<int> r) => r.OrElse(() => Result.Failure<int>(Failed()));")]
    [InlineData("Result<int> M(Result<int> r) => r.OrElse(e => Result.Failure<int>(e.WithCode(\"A\")));")]
    [InlineData("Result M(Result r) => r.OrElse(e => Rejected);")]
    [InlineData("Result M(Result r) => r.OrElse(() => Result.Failure(Rejected));")]
    [InlineData("Task<Result<int>> M(Task<Result<int>> r) => r.OrElse(e => Rejected);")]
    [InlineData("ValueTask<Result<int>> M(ValueTask<Result<int>> r) => r.OrElse(() => Failed());")]
    [InlineData("Task<Result> M(Task<Result> r) => r.OrElse(e => Rejected);")]
    [InlineData("ValueTask<Result> M(ValueTask<Result> r) => r.OrElse(e => Result.Failure(Rejected));")]
    [InlineData("Result<Error> M(Result<Error> r) => r.OrElse(e => Result.Failure<Error>(Rejected));")]
    internal async Task OrElse_that_only_returns_an_error_is_MapError(string method)
    {
        var ids = await AnalyzerTestHost.AnalyzeAsync(Sources + "\n" + method, Analyzer);

        Assert.Equal(["WYZ0118"], ids);
    }

    // The delegate parameter decides: a function named replacement returns the value, also when the value is an error (Result<Error>).
    [Theory]
    [InlineData("Result<int> M(Result<int> r) => r.OrElse(e => 0);")]
    [InlineData("Result<int> M(Result<int> r) => r.OrElse(() => 0);")]
    [InlineData("Result<Error> M(Result<Error> r) => r.OrElse(e => Rejected);")]
    [InlineData("Result<Error> M(Result<Error> r) => r.OrElse(() => Rejected);")]
    [InlineData("Task<Result<int>> M(Result<int> r) => r.OrElse(async e => Rejected);")]
    [InlineData("Task<Result<int>> M(Result<int> r) => r.OrElse(async () => Result.Failure<int>(Rejected));")]
    [InlineData("Task<Result<int>> M(Result<int> r) => r.OrElse(e => Task.FromResult(Result.Failure<int>(Rejected)));")]
    [InlineData("Result<int> M(Result<int> r) => r.OrElse(Fail);")]
    [InlineData("Result<int> M(Result<int> r) => r.OrElse(e => { Log(e); return Rejected; });")]
    [InlineData("Result<int> M(Result<int> r) => r.OrElse(e => Result.Failure<int>(\"Nope.\"));")]
    [InlineData("Result<int> M(Result<int> r) => r.OrElse(e => Result.Failure<int>(e.Message));")]
    [InlineData("Result<int> M(Result<int> r) => r.OrElse(e => Make());")]
    [InlineData("Result<int> M(Result<int> r) => r.OrElse(e => Result.Success(1));")]
    [InlineData("Result<int> M(Result<int> r) => r.OrElse((Error e) => default(Result<int>));")]
    [InlineData("Result<int> M(Result<int> r) => r.OrElse(new Func<Error, Result<int>>(e => throw new InvalidOperationException()));")]
    [InlineData("Result<int> M(Result<int> r) => ResultExtensions.OrElse(r, e => Rejected);")]
    [InlineData("Result<int> M(Result<int> r) => r.OrElse<int>(e => Rejected);")]
    [InlineData("Result<int> M(Result<int> r) => r.MapError(e => Rejected);")]
    internal async Task An_OrElse_that_does_something_else_is_left_alone(string method)
    {
        var ids = await AnalyzerTestHost.AnalyzeAsync(Sources + "\n" + method, Analyzer);

        Assert.Empty(ids);
    }

    // The fallback is cheap and does not read the error: ValueOr(fallback). Not cheap: ValueOr(() => fallback). Reads the error: ValueOr(error => fallback).
    [Theory]
    [InlineData("int M(Result<int> r) => r.IsSuccess ? r.GetValue() : 0;")]
    [InlineData("int M(Result<int> r) => (r.IsSuccess) ? r.GetValue() : 0;")]
    [InlineData("int M(Result<int> r, int fallback) => r.IsFailure ? fallback : r.GetValue();")]
    [InlineData("int M(Result<int> r) => r.IsSuccess ? r.GetValue() : default;")]
    [InlineData("int M(Result<int> r) => r.IsSuccess ? r.GetValue() : int.MaxValue;")]
    [InlineData("int M() => Field.IsSuccess ? Field.GetValue() : 0;")]
    [InlineData("int M() => this.Field.IsSuccess ? this.Field.GetValue() : 0;")]
    [InlineData("int M(Holder h) => h.Value.IsSuccess ? h.Value.GetValue() : 0;")]
    [InlineData("object M(Result<object> r, object fallback) => r.IsSuccess ? r.GetValue() : fallback;")]
    [InlineData("Func<int> M(Result<Func<int>> r, Func<int> fallback) => r.IsSuccess ? r.GetValue() : fallback;")]
    [InlineData("int M(Result<int> r) => r.IsSuccess ? r.GetValue() : Compute();")]
    [InlineData("int M(Result<int> r) => r.IsFailure ? Compute() : r.GetValue();")]
    [InlineData("string M(Result<string> r) => r.IsSuccess ? r.GetValue() : Describe();")]
    [InlineData("List<int> M(Result<List<int>> r) => r.IsSuccess ? r.GetValue() : new List<int>();")]
    [InlineData("int[] M(Result<int[]> r) => r.IsSuccess ? r.GetValue() : new int[0];")]
    [InlineData("int M(Result<int> r, int add) => r.IsSuccess ? r.GetValue() : Compute() + add;")]
    [InlineData("int M(Result<int> r, int error) => r.IsSuccess ? r.GetValue() : Compute() + error;")]
    [InlineData("int M(Result<int> r, Result<int> other) => r.IsSuccess ? r.GetValue() : other.GetError().Message.Length;")]
    [InlineData("int M(Result<int> r) => r.IsSuccess ? r.GetValue() : r.GetError().Message.Length;")]
    [InlineData("string M(Result<string> r) => r.IsFailure ? \"Failed: \" + r.GetError().Message : r.GetValue();")]
    [InlineData("int M() => Field.IsSuccess ? Field.GetValue() : Field.GetError().Message.Length;")]
    [InlineData("Error M(Result<Error> r) => r.IsSuccess ? r.GetValue() : r.GetError();")]
    internal async Task A_conditional_on_the_state_of_a_result_is_ValueOr(string method)
    {
        var ids = await AnalyzerTestHost.AnalyzeAsync(Sources + "\n" + method, Analyzer);

        Assert.Equal(["WYZ0119"], ids);
    }

    // Each of these looks like the conditional above and would change the behavior or the type, or does not compile when it is rewritten.
    [Theory]
    [InlineData("int M(Result<int> a, Result<int> b) => a.IsSuccess ? b.GetValue() : 0;")]
    [InlineData("int M() => Make().IsSuccess ? Make().GetValue() : 0;")]
    [InlineData("int M(Result<int>[] rs) => rs[0].IsSuccess ? rs[0].GetValue() : 0;")]
    [InlineData("int M(Result<int> r) => r.IsSuccess ? 0 : r.GetValue();")]
    [InlineData("int M(Result<int> r) => r.IsFailure ? r.GetValue() : 0;")]
    [InlineData("int M(Result<int> r) => r.IsSuccess ? r.GetValue() + 1 : 0;")]
    [InlineData("int M(Result<int> r) => r.IsSuccess ? GetValue(r) : 0;")]
    [InlineData("int M(Result<int> r) => r.IsSuccess ? Compute() : 0;")]
    [InlineData("int M(Result<int> r) => r.IsSuccess ? 1 : 0;")]
    [InlineData("int M(Result r) => r.IsSuccess ? 1 : 0;")]
    [InlineData("int M(Switch s) => s.IsOn ? 1 : 0;")]
    [InlineData("double M(Result<int> r) => r.IsSuccess ? r.GetValue() : 1.5;")]
    [InlineData("int M(Result<int> r) => r.IsSuccess ? r.GetValue() : throw new InvalidOperationException();")]
    [InlineData("List<int> M(Result<List<int>> r) => r.IsSuccess ? r.GetValue() : new();")]
    [InlineData("int M(Result<int> r, string s) => r.IsSuccess ? r.GetValue() : (int.TryParse(s, out var n) ? n : 0);")]
    [InlineData("int M(Result<int> r, string s) { int n; return r.IsSuccess ? r.GetValue() : (int.TryParse(s, out n) ? n : 0); }")]
    [InlineData("int M(Result<int> r, object o) => r.IsSuccess ? r.GetValue() : (o is int n ? n : Compute());")]
    [InlineData("async Task<int> M(Result<int> r) => r.IsSuccess ? r.GetValue() : await Task.FromResult(1);")]
    [InlineData("int M(Result<int> r, Span<int> span) => r.IsSuccess ? r.GetValue() : span.Length + Compute();")]
    [InlineData("int M(Result<int> r) { Span<int> span = stackalloc int[1]; return r.IsSuccess ? r.GetValue() : span.Length + Compute(); }")]
    [InlineData("int M(Result<int> r, in int x) => r.IsSuccess ? r.GetValue() : x + Compute();")]
    [InlineData("int M(Result<int> r, int[] a) { ref int x = ref a[0]; return r.IsSuccess ? r.GetValue() : x + Compute(); }")]
    [InlineData("object M(Result<object> r) => r.IsSuccess ? r.GetValue() : Compute();")]
    [InlineData("object M(Result<object> r) => r.IsSuccess ? r.GetValue() : r.GetError();")]
    [InlineData("Delegate M(Result<Delegate> r) => r.IsSuccess ? r.GetValue() : Build();")]
    [InlineData("MulticastDelegate M(Result<MulticastDelegate> r) => r.IsSuccess ? r.GetValue() : BuildMulticast();")]
    [InlineData("Func<int> M(Result<Func<int>> r) => r.IsSuccess ? r.GetValue() : Maker();")]
    [InlineData("dynamic M(Result<dynamic> r) => r.IsSuccess ? r.GetValue() : Compute();")]
    [InlineData(
        "System.Linq.Expressions.Expression<Func<int>> M(Result<System.Linq.Expressions.Expression<Func<int>>> r) =>"
        + " r.IsSuccess ? r.GetValue() : MakeExpression();")]
    [InlineData("int M(Result<int> r, int error) => r.IsSuccess ? r.GetValue() : r.GetError().Message.Length + error;")]
    [InlineData("int M(Result<int> r) { var f = r.IsSuccess ? r.GetValue() : r.GetError().Message.Length; var error = 1; return f + error; }")]
    [InlineData("int error; int M(Result<int> r) => r.IsSuccess ? r.GetValue() : r.GetError().Message.Length;")]
    [InlineData("int M(Result<int> r) => r.IsSuccess ? r.GetValue() : r.GetError().Message.Length + Apply(error => error + 1);")]
    internal async Task A_conditional_that_ValueOr_would_not_say_the_same_is_left_alone(string method)
    {
        var ids = await AnalyzerTestHost.AnalyzeAsync(Sources + "\n" + method, Analyzer);

        Assert.DoesNotContain("WYZ0119", ids);
    }

    [Fact]
    internal async Task A_conditional_in_a_struct_whose_lazy_fallback_reads_the_struct_is_left_alone_because_a_lambda_cannot_capture_it()
    {
        const string source = """
            public struct Counter
            {
                public int Count;
                public Result<int> Value;
                static int Compute() => 1;
                public int Lazy() => Value.IsSuccess ? Value.GetValue() : Count + Compute();
                public int Cheap() => Value.IsSuccess ? Value.GetValue() : Count;
            }
            """;

        var ids = await AnalyzerTestHost.AnalyzeSourceAsync(source, Analyzer);

        Assert.Equal(["WYZ0119"], ids);
    }

    [Fact]
    internal async Task A_conditional_and_a_ValueOr_on_look_alikes_of_Result_and_Maybe_are_left_alone()
    {
        const string source = """
            namespace Other
            {
                public sealed class Result<T>
                {
                    public bool IsSuccess => true;
                    public T GetValue() => default!;
                    public T ValueOr(T fallback) => fallback;
                }

                public static class Use
                {
                    static int Compute() => 1;
                    public static int Conditional(Result<int> r) => r.IsSuccess ? r.GetValue() : 0;
                    public static int Function(Result<int> r) => r.ValueOr(Compute());
                }
            }

            public readonly struct Maybe<T>
            {
                public T ValueOr(T fallback) => fallback;
            }

            public static class UseMaybe
            {
                static int Compute() => 1;
                public static int Function(Maybe<int> m) => m.ValueOr(Compute());
            }
            """;

        var ids = await AnalyzerTestHost.AnalyzeSourceAsync(source, Analyzer);

        Assert.Empty(ids);
    }

    [Theory]
    [InlineData("int M(Result<int> r) => r.ValueOr(Compute());")]
    [InlineData("string M(Result<string> r) => r.ValueOr(Describe());")]
    [InlineData("List<int> M(Result<List<int>> r) => r.ValueOr(new List<int>());")]
    [InlineData("int[] M(Result<int[]> r) => r.ValueOr(new int[0]);")]
    [InlineData("int[] M(Result<int[]> r) => r.ValueOr(new[] { 1, 2 });")]
    [InlineData("List<int> M(Result<List<int>> r) => r.ValueOr([1, 2]);")]
    [InlineData("string M(Result<string> r, int id) => r.ValueOr($\"unknown {id}\");")]
    [InlineData("DateTime M(Result<DateTime> r) => r.ValueOr(new DateTime(2020, 1, 1));")]
    [InlineData("int M(Result<int> r) => r.ValueOr(Compute() + 1);")]
    [InlineData("int M(Result<int> r) => r.ValueOr(fallback: Compute());")]
    [InlineData("int M(Result<int> r) => r.ValueOr((Compute()));")]
    [InlineData("int M(Holder h) => h.Value.ValueOr(Compute());")]
    [InlineData("Task<int> M(Task<Result<int>> r) => r.ValueOr(Compute());")]
    [InlineData("ValueTask<int> M(ValueTask<Result<int>> r) => r.ValueOr(Compute());")]
    internal async Task ValueOr_with_a_fallback_that_does_work_takes_a_function_instead(string method)
    {
        var ids = await AnalyzerTestHost.AnalyzeAsync(Sources + "\n" + method, Analyzer);

        Assert.Equal(["WYZ0120"], ids);
    }

    // Reading a local, a parameter, a field, a property or a constant is not work, and a function, or a value that a lambda would become, is left alone.
    [Theory]
    [InlineData("List<int> M(Result<List<int>> r) => r.ValueOr(new());")]
    [InlineData("int M(Result<int> r, int fallback) => r.ValueOr(fallback);")]
    [InlineData("int M(Result<int> r) => r.ValueOr(0);")]
    [InlineData("int M(Result<int> r) => r.ValueOr(int.MaxValue);")]
    [InlineData("string M(Result<string> r) => r.ValueOr(string.Empty);")]
    [InlineData("string M(Result<string> r) => r.ValueOr(Environment.NewLine);")]
    [InlineData("int M(Result<int> r) => r.ValueOr(DateTime.UtcNow.Year);")]
    [InlineData("string M(Result<string> r) => r.ValueOr($\"unknown\");")]
    [InlineData("int M(Result<int> r, Holder h) => r.ValueOr(h.Value.IsSuccess ? 1 : 2);")]
    [InlineData("int M(Result<int> r) => r.ValueOr(() => Compute());")]
    [InlineData("int M(Result<int> r) => r.ValueOr(e => Compute());")]
    [InlineData("Task<int> M(Task<Result<int>> r) => r.ValueOr(() => Compute());")]
    [InlineData("Task<int> M(Task<Result<int>> r) => ResultExtensions.ValueOr(r, Compute());")]
    [InlineData("object M(Result<object> r) => r.ValueOr(Compute());")]
    [InlineData("Func<int> M(Result<Func<int>> r) => r.ValueOr(Maker());")]
    [InlineData("async Task<int> M(Result<int> r) => r.ValueOr(await Task.FromResult(1));")]
    [InlineData("int M(Result<int> r, string s) => r.ValueOr(int.TryParse(s, out var n) ? n : 0);")]
    [InlineData("int M(Result<int> r, Span<int> span) => r.ValueOr(span.Length + Compute());")]
    internal async Task ValueOr_with_a_fallback_that_does_no_work_or_cannot_be_a_function_is_left_alone(string method)
    {
        var ids = await AnalyzerTestHost.AnalyzeAsync(Sources + "\n" + method, Analyzer);

        Assert.DoesNotContain("WYZ0120", ids);
    }
}
