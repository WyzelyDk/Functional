using Wyzely.Functional.MaybeAnalyzers;

namespace Wyzely.Functional.MaybeAnalyzersTests;

// The simpler form must say the same thing, compile, and the suggestion must be gone afterwards.
public sealed class MaybeSimplifyCodeFixTests
{
    private const string Sources = """
        static Maybe<int> Twice(int x) => Maybe.Some(x * 2);
        static Maybe<int> Find() => Maybe.Some(1);
        static int Compute() => 42;
        """;

    private static readonly MaybeSuggestionsCodeFixProvider Provider = new();
    private static readonly MaybeSuggestionsAnalyzer Analyzer = new();

    [Theory]
    [InlineData("Maybe<int> M(Maybe<int> m) => m.Bind(x => Maybe.Some(x * 2));", "m.Map(x => x * 2)")]
    [InlineData("Maybe<TStruct> M<TStruct>(Maybe<int> m, TStruct s) where TStruct : struct => m.Bind(x => Maybe.Some(s));", "m.Map(x => s)")]
    [InlineData("Maybe<TUnmanaged> M<TUnmanaged>(Maybe<int> m, TUnmanaged u) where TUnmanaged : unmanaged => m.Bind(x => Maybe.Some(u));", "m.Map(x => u)")]
    [InlineData("Maybe<int> M(Maybe<int> m) => m.Bind(x => { return Maybe.Some(x + 1); });", "m.Map(x => { return x + 1; })")]
    [InlineData("Maybe<int> M(Maybe<int> m) => m.Bind(x => Maybe.Some<int>(x + 1));", "m.Map(x => x + 1)")]
    [InlineData("Maybe<int> M(Maybe<int> m) => m.Bind(x => (Maybe.Some(x * 2)));", "m.Map(x => x * 2)")]
    [InlineData("Maybe<int> M(Maybe<int> m) => m.Bind(static x => Maybe.Some(x * 2));", "m.Map(static x => x * 2)")]
    [InlineData("Maybe<int> M(Maybe<int> m) => m.Bind((int x) => Maybe.Some(x * 2));", "m.Map((int x) => x * 2)")]
    [InlineData("Maybe<int> M(Maybe<int> m) => m.Bind(bind: x => Maybe.Some(x * 2));", "m.Map(x => x * 2)")]
    [InlineData("Maybe<int> M(Maybe<string> m) => m.Bind(s => Maybe.Some(s.Length));", "m.Map(s => s.Length)")]
    [InlineData("Maybe<bool> M(Maybe<int> m) => m.Bind(x => Maybe.Some(x > 1));", "m.Map(x => x > 1)")]
    [InlineData("Maybe<DateTime> M(Maybe<int> m) => m.Bind(x => Maybe.Some(new DateTime(x, 1, 1)));", "m.Map(x => new DateTime(x, 1, 1))")]
    [InlineData("Maybe<(int, int)> M(Maybe<int> m) => m.Bind(x => Maybe.Some((x, 1)));", "m.Map(x => (x, 1))")]
    [InlineData("Maybe<DayOfWeek> M(Maybe<int> m) => m.Bind(x => Maybe.Some(DayOfWeek.Monday));", "m.Map(x => DayOfWeek.Monday)")]
    [InlineData("Maybe<int> M() => Find().Bind(x => Maybe.Some(x * 2));", "Find().Map(x => x * 2)")]
    [InlineData("Task<Maybe<int>> M(Task<Maybe<int>> m) => m.Bind(x => Maybe.Some(x * 2));", "m.Map(x => x * 2)")]
    [InlineData("ValueTask<Maybe<int>> M(ValueTask<Maybe<int>> m) => m.Bind(x => Maybe.Some(x * 2));", "m.Map(x => x * 2)")]
    internal async Task Bind_that_only_wraps_a_value_in_Some_becomes_Map(string method, string expected)
    {
        var outcome = await CodeFixTestHost.FixAsync(Sources + "\n" + method, Provider, "WYZ0206", Analyzer);

        Assert.Multiple(
            () => Assert.NotNull(outcome.FixedSource),
            () => Assert.Contains(expected, outcome.FixedSource, StringComparison.Ordinal),
            () => Assert.DoesNotContain("Bind", outcome.FixedSource, StringComparison.Ordinal),
            () => Assert.DoesNotContain("WYZ0206", outcome.RemainingIds),
            () => Assert.Empty(CodeFixTestHost.CompilerErrors(outcome.FixedSource)));
    }

    [Theory]
    [InlineData("Maybe<int> M(Maybe<int> m) => m.Map(x => x);", "=> m;")]
    [InlineData("Maybe<int> M(Maybe<int> m) => m.Map(x => { return x; });", "=> m;")]
    [InlineData("Maybe<string> M(Maybe<string> m) => m.Map(x => x);", "=> m;")]
    [InlineData("Maybe<int> M() => Find().Map(x => x);", "=> Find();")]
    [InlineData("Task<Maybe<int>> M(Task<Maybe<int>> m) => m.Map(x => x);", "=> m;")]
    [InlineData("ValueTask<Maybe<int>> M(ValueTask<Maybe<int>> m) => m.Map(x => x);", "=> m;")]
    [InlineData("Maybe<int> M(Maybe<int> m) => m.Bind(x => Maybe.Some(x));", "=> m;")]
    [InlineData("Task<Maybe<int>> M(Task<Maybe<int>> m) => m.Bind(x => Maybe.Some(x));", "=> m;")]
    [InlineData("Maybe<int> M(Maybe<int> m) => m.OrElse(() => Maybe.None<int>());", "=> m;")]
    [InlineData("Maybe<int> M(Maybe<int> m) => m.OrElse(() => default(Maybe<int>));", "=> m;")]
    [InlineData("Task<Maybe<int>> M(Task<Maybe<int>> m) => m.OrElse(() => default(Maybe<int>));", "=> m;")]
    [InlineData("Maybe<int> M(Maybe<int> m) => m.OrElse(() => { return Maybe.None<int>(); });", "=> m;")]
    [InlineData("Maybe<string> M(Maybe<string> m) => m.OrElse(() => Maybe.None<string>());", "=> m;")]
    [InlineData("Task<Maybe<int>> M(Task<Maybe<int>> m) => m.OrElse(() => Maybe.None<int>());", "=> m;")]
    [InlineData("ValueTask<Maybe<int>> M(ValueTask<Maybe<int>> m) => m.OrElse(() => Maybe.None<int>());", "=> m;")]
    [InlineData("Maybe<int> M(Maybe<int> m) => m.Bind(Twice).Map(x => x).Map(x => x + 1);", "m.Bind(Twice).Map(x => x + 1)")]
    [InlineData("Maybe<int> M(Maybe<int> m) => m.Bind(Twice).OrElse(() => Maybe.None<int>()).Map(x => x + 1);", "m.Bind(Twice).Map(x => x + 1)")]
    [InlineData("bool M(Maybe<int> m) => m.Map(x => x).IsSome;", "m.IsSome")]
    internal async Task A_step_that_leaves_the_maybe_as_it_was_is_removed(string method, string expected)
    {
        var outcome = await CodeFixTestHost.FixAsync(Sources + "\n" + method, Provider, "WYZ0207", Analyzer);

        Assert.Multiple(
            () => Assert.NotNull(outcome.FixedSource),
            () => Assert.Contains(expected, outcome.FixedSource, StringComparison.Ordinal),
            () => Assert.DoesNotContain("WYZ0207", outcome.RemainingIds),
            () => Assert.Empty(CodeFixTestHost.CompilerErrors(outcome.FixedSource)));
    }
}
