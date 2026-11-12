using Wyzely.Functional.MaybeAnalyzers;

namespace Wyzely.Functional.MaybeAnalyzersTests;

// The rewritten code must say the same thing, compile, and the suggestion must be gone afterwards.
public sealed class MaybeValueOrCodeFixTests
{
    private const string Sources = """
        static int Compute() => 42;
        static Maybe<int> Find() => Maybe.Some(1);
        static Func<int> Maker() => Compute;
        static Task<object> Later() => Task.FromResult<object>(1);
        sealed record Holder(Maybe<int> Value);
        Maybe<int> Field = Maybe.Some(2);
        """;

    private static readonly MaybeSuggestionsCodeFixProvider Provider = new();
    private static readonly MaybeSuggestionsAnalyzer Analyzer = new();

    [Theory]
    [InlineData("int M(Maybe<int> m) => m.Match(v => v, () => 0);", "m.ValueOr(0)")]
    [InlineData("int M(Maybe<int> m) => m.Match(v => v, () => { return 0; });", "m.ValueOr(0)")]
    [InlineData("string M(Maybe<string> m, string fallback) => m.Match(v => v, () => fallback);", "m.ValueOr(fallback)")]
    [InlineData("int M(Maybe<int> m) => m.Match(onSome: v => v, onNone: () => 0);", "m.ValueOr(0)")]
    [InlineData("int M(Maybe<int> m) => m.Match(onNone: () => 0, onSome: v => v);", "m.ValueOr(0)")]
    [InlineData("int M(Maybe<int> m) => m.Match(v => v, () => (0));", "m.ValueOr(0)")]
    [InlineData("object M(Maybe<object> m, object fallback) => m.Match(v => v, () => fallback);", "m.ValueOr(fallback)")]
    [InlineData("int M(Holder h) => h.Value.Match(v => v, () => 0);", "h.Value.ValueOr(0)")]
    [InlineData("int M(Maybe<int> m) => m.Match(v => v, () => Compute());", "m.ValueOr(() => Compute())")]
    [InlineData("int M(Maybe<int> m) => m.Match(v => v, () => { return Compute(); });", "m.ValueOr(() => { return Compute(); })")]
    [InlineData("int M(Maybe<int> m) => m.Match(v => v, static () => Compute());", "m.ValueOr(static () => Compute())")]
    [InlineData("int M(Maybe<int> m) => m.Match(onSome: v => v, onNone: () => Compute());", "m.ValueOr(() => Compute())")]
    [InlineData("List<int> M(Maybe<List<int>> m) => m.Match(v => v, () => new List<int>());", "m.ValueOr(() => new List<int>())")]
    [InlineData("int M() => Find().Match(v => v, () => Compute());", "Find().ValueOr(() => Compute())")]
    [InlineData("Task<int> M(Task<Maybe<int>> m) => m.Match(v => v, () => 0);", "m.ValueOr(() => 0)")]
    [InlineData("Task<int> M(Task<Maybe<int>> m) => m.Match(v => v, () => default);", "m.ValueOr(() => default)")]
    [InlineData("Task<string> M(Task<Maybe<string>> m) => m.Match(v => v, () => null!);", "m.ValueOr(() => null!)")]
    [InlineData("Task<string> M(Task<Maybe<string>> m) => m.Match(v => v, () => null);", "m.ValueOr(() => null)")]
    [InlineData("Task<int> M(Task<Maybe<int>> m) => m.Match(v => v, () => default(int));", "m.ValueOr(() => default(int))")]
    [InlineData("Task<List<int>> M(Task<Maybe<List<int>>> m) => m.Match(v => v, () => [1]);", "m.ValueOr(() => [1])")]
    [InlineData("Task<int> M(Task<Maybe<int>> m) => m.Match(v => v, () => Compute());", "m.ValueOr(() => Compute())")]
    [InlineData("ValueTask<int> M(ValueTask<Maybe<int>> m) => m.Match(v => v, () => 0);", "m.ValueOr(() => 0)")]
    [InlineData("ValueTask<int> M(ValueTask<Maybe<int>> m) => m.Match(v => v, () => Compute());", "m.ValueOr(() => Compute())")]
    [InlineData("Task<Func<int>> M(Task<Maybe<Func<int>>> m) => m.Match(v => v, () => Maker());", "m.ValueOr(() => Maker())")]
    internal async Task Match_that_returns_the_value_or_a_fallback_becomes_ValueOr(string method, string expected)
    {
        var outcome = await CodeFixTestHost.FixAsync(Sources + "\n" + method, Provider, "WYZ0205", Analyzer);

        Assert.Multiple(
            () => Assert.NotNull(outcome.FixedSource),
            () => Assert.Contains(expected, outcome.FixedSource, StringComparison.Ordinal),
            () => Assert.DoesNotContain("Match", outcome.FixedSource, StringComparison.Ordinal),
            () => Assert.DoesNotContain("WYZ0205", outcome.RemainingIds),
            () => Assert.Empty(CodeFixTestHost.CompilerErrors(outcome.FixedSource)));
    }

    [Theory]
    [InlineData("int M(Maybe<int> m) => m.IsSome ? m.GetValue() : Compute();", "m.ValueOr(() => Compute())")]
    [InlineData("int M(Maybe<int> m) => m.IsNone ? Compute() : m.GetValue();", "m.ValueOr(() => Compute())")]
    [InlineData("int M(Maybe<int> m) => m.IsSome ? m.GetValue() : (Compute());", "m.ValueOr(() => Compute())")]
    [InlineData("List<int> M(Maybe<List<int>> m) => m.IsSome ? m.GetValue() : new List<int>();", "m.ValueOr(() => new List<int>())")]
    [InlineData("int M(Maybe<int> m, int add) => m.IsSome ? m.GetValue() : Compute() + add;", "m.ValueOr(() => Compute() + add)")]
    [InlineData("string M(Maybe<string> m, int id) => m.IsSome ? m.GetValue() : $\"unknown {id}\";", "m.ValueOr(() => $\"unknown {id}\")")]
    [InlineData("int M() => Field.IsSome ? Field.GetValue() : Compute();", "Field.ValueOr(() => Compute())")]
    [InlineData("int M(Maybe<int> m, Maybe<int> other) => m.IsSome ? m.GetValue() : other.GetValue();", "m.ValueOr(() => other.GetValue())")]
    [InlineData("Task<object> M(Maybe<Task<object>> m) => m.IsSome ? m.GetValue() : Later();", "m.ValueOr(() => Later())")]
    internal async Task A_conditional_on_IsSome_with_a_fallback_that_does_work_becomes_ValueOr_with_a_function(string method, string expected)
    {
        var outcome = await CodeFixTestHost.FixAsync(Sources + "\n" + method, Provider, "WYZ0203", Analyzer);

        Assert.Multiple(
            () => Assert.NotNull(outcome.FixedSource),
            () => Assert.Contains(expected, outcome.FixedSource, StringComparison.Ordinal),
            () => Assert.DoesNotContain("?", outcome.FixedSource, StringComparison.Ordinal),
            () => Assert.DoesNotContain("WYZ0203", outcome.RemainingIds),
            () => Assert.Empty(CodeFixTestHost.CompilerErrors(outcome.FixedSource)));
    }

    [Theory]
    [InlineData("int M(Maybe<int> m) => m.IsSome ? m.GetValue() : 0;", "m.ValueOr(0)")]
    [InlineData("int M(Maybe<int> m) => m.IsNone ? 0 : m.GetValue();", "m.ValueOr(0)")]
    [InlineData("int M(Maybe<int> m, int fallback) => m.IsSome ? m.GetValue() : fallback;", "m.ValueOr(fallback)")]
    [InlineData("object M(Maybe<object> m, object fallback) => m.IsSome ? m.GetValue() : fallback;", "m.ValueOr(fallback)")]
    internal async Task A_conditional_on_IsSome_with_a_cheap_fallback_still_becomes_ValueOr_with_the_value(string method, string expected)
    {
        var outcome = await CodeFixTestHost.FixAsync(Sources + "\n" + method, Provider, "WYZ0203", Analyzer);

        Assert.Multiple(
            () => Assert.NotNull(outcome.FixedSource),
            () => Assert.Contains(expected, outcome.FixedSource, StringComparison.Ordinal),
            () => Assert.DoesNotContain("ValueOr(()", outcome.FixedSource, StringComparison.Ordinal),
            () => Assert.DoesNotContain("WYZ0203", outcome.RemainingIds),
            () => Assert.Empty(CodeFixTestHost.CompilerErrors(outcome.FixedSource)));
    }

    [Theory]
    [InlineData("int M(Maybe<int> m) => m.ValueOr(Compute());", "m.ValueOr(() => Compute())")]
    [InlineData("int M(Maybe<int> m) => m.ValueOr((Compute()));", "m.ValueOr(() => Compute())")]
    [InlineData("int M(Maybe<int> m) => m.ValueOr(fallback: Compute());", "m.ValueOr(fallback: () => Compute())")]
    [InlineData("int M(Maybe<int> m) => m.ValueOr(Compute() + 1);", "m.ValueOr(() => Compute() + 1)")]
    [InlineData("List<int> M(Maybe<List<int>> m) => m.ValueOr(new List<int>());", "m.ValueOr(() => new List<int>())")]
    [InlineData("List<int> M(Maybe<List<int>> m) => m.ValueOr([1, 2]);", "m.ValueOr(() => [1, 2])")]
    [InlineData("int[] M(Maybe<int[]> m) => m.ValueOr(new[] { 1, 2 });", "m.ValueOr(() => new[] { 1, 2 })")]
    [InlineData("string M(Maybe<string> m, int id) => m.ValueOr($\"unknown {id}\");", "m.ValueOr(() => $\"unknown {id}\")")]
    [InlineData("int M(Holder h) => h.Value.ValueOr(Compute());", "h.Value.ValueOr(() => Compute())")]
    [InlineData("int M() => Find().ValueOr(Compute());", "Find().ValueOr(() => Compute())")]
    [InlineData("int M() => Field.ValueOr(Compute());", "Field.ValueOr(() => Compute())")]
    internal async Task ValueOr_with_a_fallback_that_does_work_gets_a_function(string method, string expected)
    {
        var outcome = await CodeFixTestHost.FixAsync(Sources + "\n" + method, Provider, "WYZ0208", Analyzer);

        Assert.Multiple(
            () => Assert.NotNull(outcome.FixedSource),
            () => Assert.Contains(expected, outcome.FixedSource, StringComparison.Ordinal),
            () => Assert.DoesNotContain("WYZ0208", outcome.RemainingIds),
            () => Assert.Empty(CodeFixTestHost.CompilerErrors(outcome.FixedSource)));
    }
}
