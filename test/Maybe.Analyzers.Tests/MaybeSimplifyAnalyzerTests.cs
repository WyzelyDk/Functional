using Wyzely.Functional.MaybeAnalyzers;

namespace Wyzely.Functional.MaybeAnalyzersTests;

// The rules about code that does more than it has to: Map instead of a Bind that only wraps the value in Some (WYZ0206),
// and steps that leave the maybe as it was (WYZ0207). A rewrite must give exactly the same maybe, so a shape that looks alike but is not stays silent.
public sealed class MaybeSimplifyAnalyzerTests
{
    private const string Sources = """
        static Maybe<int> Twice(int x) => Maybe.Some(x * 2);
        static Maybe<int> Find() => Maybe.Some(1);
        static int Compute() => 42;
        static Func<int, int> Double() => x => x * 2;
        """;

    private static readonly MaybeSuggestionsAnalyzer Analyzer = new();

    [Theory]
    [InlineData("Maybe<int> M(Maybe<int> m) => m.Bind(x => Maybe.Some(x * 2));")]
    [InlineData("Maybe<int> M(Maybe<int> m) => m.Bind(x => { return Maybe.Some(x + 1); });")]
    [InlineData("Maybe<int> M(Maybe<int> m) => m.Bind(x => Maybe.Some<int>(x + 1));")]
    [InlineData("Maybe<int> M(Maybe<int> m) => m.Bind(x => Maybe.Some(Compute()));")]
    [InlineData("Maybe<int> M(Maybe<int> m) => m.Bind(static x => Maybe.Some(x * 2));")]
    [InlineData("Maybe<int> M(Maybe<string> m) => m.Bind(s => Maybe.Some(s.Length));")]
    [InlineData("Maybe<bool> M(Maybe<int> m) => m.Bind(x => Maybe.Some(x > 1));")]
    [InlineData("Maybe<DateTime> M(Maybe<int> m) => m.Bind(x => Maybe.Some(new DateTime(x, 1, 1)));")]
    [InlineData("Maybe<(int, int)> M(Maybe<int> m) => m.Bind(x => Maybe.Some((x, 1)));")]
    [InlineData("Maybe<DayOfWeek> M(Maybe<int> m) => m.Bind(x => Maybe.Some(DayOfWeek.Monday));")]
    [InlineData("Maybe<TStruct> M<TStruct>(Maybe<int> m, TStruct s) where TStruct : struct => m.Bind(x => Maybe.Some(s));")]
    [InlineData("Maybe<TUnmanaged> M<TUnmanaged>(Maybe<int> m, TUnmanaged u) where TUnmanaged : unmanaged => m.Bind(x => Maybe.Some(u));")]
    [InlineData("Maybe<int> M() => Find().Bind(x => Maybe.Some(x * 2));")]
    [InlineData("Task<Maybe<int>> M(Task<Maybe<int>> m) => m.Bind(x => Maybe.Some(x * 2));")]
    [InlineData("ValueTask<Maybe<int>> M(ValueTask<Maybe<int>> m) => m.Bind(x => Maybe.Some(x * 2));")]
    internal async Task Bind_that_only_wraps_a_value_type_in_Some_is_Map(string method)
    {
        var ids = await AnalyzerTestHost.AnalyzeAsync(Sources + "\n" + method, Analyzer);

        Assert.Equal(["WYZ0206"], ids);
    }

    // Some throws for null and Map turns null into none, so only an expression that can never be null may change from the one to the other.
    [Theory]
    [InlineData("Maybe<string> M(Maybe<int> m) => m.Bind(x => Maybe.Some(x.ToString()));")]
    [InlineData("Maybe<string> M(Maybe<string> m) => m.Bind(s => Maybe.Some(s.Trim()));")]
    [InlineData("Maybe<List<int>> M(Maybe<int> m) => m.Bind(x => Maybe.Some(new List<int> { x }));")]
    [InlineData("Maybe<int?> M(Maybe<int> m) => m.Bind(x => Maybe.Some((int?)x));")]
    [InlineData("Maybe<T> M<T>(Maybe<T> m, Func<T, T> f) where T : notnull => m.Bind(x => Maybe.Some(f(x)));")]
    [InlineData("Maybe<TClass> M<TClass>(Maybe<int> m, TClass c) where TClass : class => m.Bind(x => Maybe.Some(c));")]
    [InlineData("Maybe<object> M(Maybe<int> m) => m.Bind(x => Maybe.Some<object>(x));")]
    [InlineData("Maybe<long> M(Maybe<int> m) => m.Bind(x => Maybe.Some<long>(x));")]
    [InlineData("Maybe<int> M(Maybe<int> m) => m.Bind(x => Twice(x));")]
    [InlineData("Maybe<int> M(Maybe<int> m) => m.Bind(Twice);")]
    [InlineData("Maybe<int> M(Maybe<int> m) => m.Bind(x => Maybe.None<int>());")]
    [InlineData("Maybe<int> M(Maybe<int> m) => m.Bind(x => Maybe.SomeIf(x > 1, x));")]
    [InlineData("Maybe<int> M(Maybe<int> m) => m.Bind(x => x > 1 ? Maybe.Some(x) : Maybe.None<int>());")]
    [InlineData("Maybe<int> M(Maybe<int> m) => m.Bind(x => { Compute(); return Maybe.Some(x); });")]
    [InlineData("Task<Maybe<int>> M(Maybe<int> m) => m.Bind(async x => Maybe.Some(await Task.FromResult(x)));")]
    [InlineData("Task<Maybe<int>> M(Maybe<int> m) => m.Bind(x => Task.FromResult(Maybe.Some(x * 2)));")]
    [InlineData("Maybe<int> M(Maybe<int> m) => MaybeExtensions.Bind(m, x => Maybe.Some(x * 2));")]
    [InlineData("Maybe<int> M(Maybe<int> m) => m.Bind<int, int>(x => Maybe.Some(x * 2));")]
    internal async Task Bind_that_does_something_else_or_could_wrap_null_is_left_alone(string method)
    {
        var ids = await AnalyzerTestHost.AnalyzeAsync(Sources + "\n" + method, Analyzer);

        Assert.Empty(ids);
    }

    [Theory]
    [InlineData("Maybe<int> M(Maybe<int> m) => m.Map(x => x);")]
    [InlineData("Maybe<int> M(Maybe<int> m) => m.Map(x => { return x; });")]
    [InlineData("Maybe<int> M(Maybe<int> m) => m.Map(static x => x);")]
    [InlineData("Maybe<string> M(Maybe<string> m) => m.Map(x => x);")]
    [InlineData("Maybe<int> M() => Find().Map(x => x);")]
    [InlineData("Task<Maybe<int>> M(Task<Maybe<int>> m) => m.Map(x => x);")]
    [InlineData("ValueTask<Maybe<int>> M(ValueTask<Maybe<int>> m) => m.Map(x => x);")]
    [InlineData("Maybe<int> M(Maybe<int> m) => m.Bind(x => Maybe.Some(x));")]
    [InlineData("Maybe<int> M(Maybe<int> m) => m.Bind(x => { return Maybe.Some(x); });")]
    [InlineData("Maybe<string> M(Maybe<string> m) => m.Bind(x => Maybe.Some(x));")]
    [InlineData("Task<Maybe<int>> M(Task<Maybe<int>> m) => m.Bind(x => Maybe.Some(x));")]
    [InlineData("ValueTask<Maybe<int>> M(ValueTask<Maybe<int>> m) => m.Bind(x => Maybe.Some(x));")]
    [InlineData("Maybe<int> M(Maybe<int> m) => m.OrElse(() => Maybe.None<int>());")]
    [InlineData("Maybe<int> M(Maybe<int> m) => m.OrElse(() => default(Maybe<int>));")]
    [InlineData("Maybe<string> M(Maybe<string> m) => m.OrElse(() => { return default(Maybe<string>); });")]
    [InlineData("Task<Maybe<int>> M(Task<Maybe<int>> m) => m.OrElse(() => default(Maybe<int>));")]
    [InlineData("Maybe<int> M(Maybe<int> m) => m.OrElse(() => { return Maybe.None<int>(); });")]
    [InlineData("Maybe<int> M(Maybe<int> m) => m.OrElse(static () => Maybe.None<int>());")]
    [InlineData("Maybe<string> M(Maybe<string> m) => m.OrElse(() => Maybe.None<string>());")]
    [InlineData("Task<Maybe<int>> M(Task<Maybe<int>> m) => m.OrElse(() => Maybe.None<int>());")]
    [InlineData("ValueTask<Maybe<int>> M(ValueTask<Maybe<int>> m) => m.OrElse(() => Maybe.None<int>());")]
    [InlineData("Maybe<int> M(Maybe<int> m) => m.Bind(Twice).Map(x => x).Map(x => x + 1);")]
    internal async Task A_step_that_leaves_the_maybe_as_it_was_is_reported(string method)
    {
        var ids = await AnalyzerTestHost.AnalyzeAsync(Sources + "\n" + method, Analyzer);

        Assert.Equal(["WYZ0207"], ids);
    }

    [Theory]
    [InlineData("Maybe<int> M(Maybe<int> m) => m.Map(x => x + 0);")]
    [InlineData("Maybe<object> M(Maybe<int> m) => m.Map(x => (object)x);")]
    [InlineData("Maybe<object> M(Maybe<int> m) => m.Map<int, object>(x => x);")]
    [InlineData("Maybe<int> M(Maybe<int> m) => m.Map(x => { Compute(); return x; });")]
    [InlineData("Maybe<int> M(Maybe<int> m) => m.Map(x => Compute());")]
    [InlineData("Maybe<int> M(Maybe<int> m) => m.Map(Double());")]
    [InlineData("Maybe<int> M(Maybe<int> m, Maybe<int> other) => m.Map(x => other.GetValue());")]
    [InlineData("Task<Maybe<int>> M(Maybe<int> m) => m.Map(async x => x);")]
    [InlineData("Task<Maybe<int>> M(Maybe<int> m) => m.Map(x => Task.FromResult(x));")]
    [InlineData("Maybe<int> M(Maybe<int> m) => MaybeExtensions.Map(m, x => x);")]
    [InlineData("Maybe<int> M(Maybe<int> m) => m.Map<int, int>(x => x);")]
    [InlineData("Maybe<object> M(Maybe<int> m) => m.Bind(x => Maybe.Some<object>(x));")]
    [InlineData("Task<Maybe<int>> M(Maybe<int> m) => m.Bind(async x => Maybe.Some(x));")]
    [InlineData("Maybe<int> M(Maybe<int> m) => MaybeExtensions.Bind(m, x => Maybe.Some(x));")]
    [InlineData("Maybe<int> M(Maybe<int> m) => m.Bind(x => Maybe.Some(1));")]
    [InlineData("Maybe<int> M(Maybe<int> m) => m.OrElse(() => Maybe.Some(1));")]
    [InlineData("Maybe<int> M(Maybe<int> m) => m.OrElse(() => Find());")]
    [InlineData("Maybe<int> M(Maybe<int> m) => m.OrElse(Find);")]
    [InlineData("Maybe<int> M(Maybe<int> m) => m.OrElse(() => 0);")]
    [InlineData("Maybe<int> M(Maybe<int> m) => m.OrElse(() => { Compute(); return Maybe.None<int>(); });")]
    [InlineData("Maybe<object> M(Maybe<object> m) => m.OrElse(() => Maybe.None<int>());")]
    [InlineData("Task<Maybe<int>> M(Maybe<int> m) => m.OrElse(async () => Maybe.None<int>());")]
    [InlineData("Task<Maybe<int>> M(Maybe<int> m) => m.OrElse(() => Task.FromResult(Maybe.None<int>()));")]
    [InlineData("Maybe<int> M(Maybe<int> m) => MaybeExtensions.OrElse(m, () => Maybe.None<int>());")]
    [InlineData("Maybe<int> M(Maybe<int> m) => m.OrElse<int>(() => Maybe.None<int>());")]
    [InlineData("Maybe<int> M(Maybe<int> m) => m.Filter(x => x > 1);")]
    internal async Task A_step_that_does_something_else_is_left_alone(string method)
    {
        var ids = await AnalyzerTestHost.AnalyzeAsync(Sources + "\n" + method, Analyzer);

        Assert.DoesNotContain("WYZ0207", ids);
    }

    [Fact]
    internal async Task A_Bind_and_a_Map_on_look_alikes_of_Maybe_are_left_alone()
    {
        const string source = """
            namespace Other
            {
                public readonly struct Maybe<T>
                {
                    public Maybe<TResult> Bind<TResult>(Func<T, Maybe<TResult>> bind) => default;
                    public Maybe<TResult> Map<TResult>(Func<T, TResult> map) => default;
                    public Maybe<T> OrElse(Func<Maybe<T>> alternative) => this;
                }

                public static class Maybe
                {
                    public static Maybe<T> Some<T>(T value) => default;
                    public static Maybe<T> None<T>() => default;
                }

                public static class Use
                {
                    public static Maybe<int> Bound(Maybe<int> m) => m.Bind(x => Maybe.Some(x * 2));
                    public static Maybe<int> Mapped(Maybe<int> m) => m.Map(x => x);
                    public static Maybe<int> Else(Maybe<int> m) => m.OrElse(() => Maybe.None<int>());
                }
            }
            """;

        var ids = await AnalyzerTestHost.AnalyzeSourceAsync(source, Analyzer);

        Assert.Empty(ids);
    }

    [Fact]
    internal async Task A_Some_of_another_type_in_a_bind_of_the_library_is_not_the_Some_of_the_library()
    {
        const string source = """
            static class Mine { public static Maybe<T> Some<T>(T value) where T : notnull => default; }
            public class Sample { Maybe<int> M(Maybe<int> m) => m.Bind(x => Mine.Some(x * 2)); }
            """;

        var ids = await AnalyzerTestHost.AnalyzeSourceAsync(source, Analyzer);

        Assert.Empty(ids);
    }
}
