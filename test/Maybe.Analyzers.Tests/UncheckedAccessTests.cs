namespace Wyzely.Functional.MaybeAnalyzersTests;

public sealed class UncheckedAccessTests
{
    private const string Sources = """
        static Maybe<int> Compute() => Maybe.Some(1);
        static Task<Maybe<int>> ComputeAsync() => Task.FromResult(Maybe.Some(1));
        static List<int> Values = new();
        """;

    [Fact]
    internal async Task GetValue_without_a_check_is_reported()
    {
        var ids = await AnalyzerTestHost.AnalyzeAsync(Sources + "\nint M(Maybe<int> m) => m.GetValue();");

        Assert.Equal(["WYZ0201"], ids);
    }

    [Fact]
    internal async Task GetValue_of_a_reference_type_without_a_check_is_reported()
    {
        var ids = await AnalyzerTestHost.AnalyzeAsync(Sources + "\nstring M(Maybe<string> m) => m.GetValue();");

        Assert.Equal(["WYZ0201"], ids);
    }

    [Fact]
    internal async Task An_access_on_the_result_of_a_call_cannot_be_guarded_and_is_reported()
    {
        var ids = await AnalyzerTestHost.AnalyzeAsync(Sources + "\nint M() => Compute().GetValue();");

        Assert.Equal(["WYZ0201"], ids);
    }

    [Fact]
    internal async Task An_access_on_an_awaited_call_is_reported()
    {
        var ids = await AnalyzerTestHost.AnalyzeAsync(Sources + "\nasync Task<int> M() => (await ComputeAsync()).GetValue();");

        Assert.Equal(["WYZ0201"], ids);
    }

    [Fact]
    internal async Task An_access_on_an_awaited_variable_without_a_check_is_reported()
    {
        var ids = await AnalyzerTestHost.AnalyzeAsync(Sources + "\nasync Task<int> M(Task<Maybe<int>> t) => (await t).GetValue();");

        Assert.Equal(["WYZ0201"], ids);
    }

    [Fact]
    internal async Task An_access_on_an_awaited_variable_after_a_check_of_the_same_expression_is_not_reported()
    {
        var ids = await AnalyzerTestHost.AnalyzeAsync(Sources + "\nasync Task<int> M(Task<Maybe<int>> t) => (await t).IsSome ? (await t).GetValue() : 0;");

        Assert.Empty(ids);
    }

    [Fact]
    internal async Task An_access_on_a_maybe_that_was_awaited_into_a_variable_after_a_check_is_not_reported()
    {
        var ids = await AnalyzerTestHost.AnalyzeAsync(Sources + "\nasync Task<int> M() { var m = await ComputeAsync(); return m.IsSome ? m.GetValue() : 0; }");

        Assert.Empty(ids);
    }

    [Fact]
    internal async Task GetValue_inside_an_if_on_IsSome_is_not_reported()
    {
        var ids = await AnalyzerTestHost.AnalyzeAsync(Sources + "\nint M(Maybe<int> m) { if (m.IsSome) { return m.GetValue(); } return 0; }");

        Assert.Empty(ids);
    }

    [Fact]
    internal async Task GetValue_inside_an_if_on_a_negated_IsNone_is_not_reported()
    {
        var ids = await AnalyzerTestHost.AnalyzeAsync(Sources + "\nint M(Maybe<int> m) { if (!m.IsNone) { return m.GetValue(); } return 0; }");

        Assert.Empty(ids);
    }

    [Fact]
    internal async Task GetValue_in_the_else_branch_of_an_if_on_IsNone_is_not_reported()
    {
        var ids = await AnalyzerTestHost.AnalyzeAsync(Sources + "\nint M(Maybe<int> m) { if (m.IsNone) { return 0; } else { return m.GetValue(); } }");

        Assert.Empty(ids);
    }

    [Fact]
    internal async Task GetValue_after_an_early_exit_on_IsNone_is_not_reported()
    {
        var ids = await AnalyzerTestHost.AnalyzeAsync(Sources + "\nint M(Maybe<int> m) { if (m.IsNone) { return 0; } return m.GetValue(); }");

        Assert.Empty(ids);
    }

    [Fact]
    internal async Task GetValue_after_an_early_return_without_braces_on_IsNone_is_not_reported()
    {
        var ids = await AnalyzerTestHost.AnalyzeAsync(Sources + "\nint M(Maybe<int> m) { if (m.IsNone) return 0; return m.GetValue(); }");

        Assert.Empty(ids);
    }

    [Fact]
    internal async Task GetValue_after_an_early_exit_on_a_negated_IsSome_is_not_reported()
    {
        var ids = await AnalyzerTestHost.AnalyzeAsync(Sources + "\nint M(Maybe<int> m) { if (!m.IsSome) throw new InvalidOperationException(); " +
            "return m.GetValue(); }");

        Assert.Empty(ids);
    }

    [Fact]
    internal async Task GetValue_after_a_continue_on_IsNone_is_not_reported()
    {
        var ids = await AnalyzerTestHost.AnalyzeAsync(
            Sources + "\nvoid M(List<Maybe<int>> ms) { foreach (var m in ms) { if (m.IsNone) { continue; } Values.Add(m.GetValue()); } }");

        Assert.Empty(ids);
    }

    [Fact]
    internal async Task GetValue_in_the_true_branch_of_a_conditional_on_IsSome_is_not_reported()
    {
        var ids = await AnalyzerTestHost.AnalyzeAsync(Sources + "\nint M(Maybe<int> m) => m.IsSome ? m.GetValue() : 0;");

        Assert.Empty(ids);
    }

    [Fact]
    internal async Task GetValue_in_the_false_branch_of_a_conditional_on_IsNone_is_not_reported()
    {
        var ids = await AnalyzerTestHost.AnalyzeAsync(Sources + "\nint M(Maybe<int> m) => m.IsNone ? 0 : m.GetValue();");

        Assert.Empty(ids);
    }

    [Fact]
    internal async Task GetValue_to_the_right_of_an_and_on_IsSome_is_not_reported()
    {
        var ids = await AnalyzerTestHost.AnalyzeAsync(Sources + "\nbool M(Maybe<int> m) => m.IsSome && m.GetValue() > 1;");

        Assert.Empty(ids);
    }

    [Fact]
    internal async Task GetValue_to_the_right_of_an_or_on_IsNone_is_not_reported()
    {
        var ids = await AnalyzerTestHost.AnalyzeAsync(Sources + "\nbool M(Maybe<int> m) => m.IsNone || m.GetValue() > 1;");

        Assert.Empty(ids);
    }

    [Fact]
    internal async Task GetValue_guarded_by_one_operand_of_a_longer_and_is_not_reported()
    {
        var ids = await AnalyzerTestHost.AnalyzeAsync(Sources + "\nint M(Maybe<int> m, bool other) { if (other && m.IsSome) { return m.GetValue(); " +
            "} return 0; }");

        Assert.Empty(ids);
    }

    [Fact]
    internal async Task GetValue_after_a_successful_TryGetValue_is_not_reported()
    {
        var ids = await AnalyzerTestHost.AnalyzeAsync(Sources + "\nint M(Maybe<int> m) { if (m.TryGetValue(out _)) { return m.GetValue(); } return 0; }");

        Assert.Empty(ids);
    }

    [Fact]
    internal async Task GetValue_after_an_early_exit_on_a_failed_TryGetValue_is_not_reported()
    {
        var ids = await AnalyzerTestHost.AnalyzeAsync(Sources + "\nint M(Maybe<int> m) { if (!m.TryGetValue(out _)) { return 0; } return m.GetValue(); }");

        Assert.Empty(ids);
    }

    [Fact]
    internal async Task GetValue_of_a_field_after_a_check_of_the_same_field_is_not_reported()
    {
        var ids = await AnalyzerTestHost.AnalyzeAsync("Maybe<int> _field;\nint M() { if (_field.IsSome) { return _field.GetValue(); } return 0; }");

        Assert.Empty(ids);
    }

    [Fact]
    internal async Task The_wrong_check_does_not_guard_the_access()
    {
        var ids = await AnalyzerTestHost.AnalyzeAsync(Sources + "\nint M(Maybe<int> m) { if (m.IsNone) { return m.GetValue(); } return 0; }");

        Assert.Equal(["WYZ0201"], ids);
    }

    [Fact]
    internal async Task The_wrong_negation_does_not_guard_the_access()
    {
        var ids = await AnalyzerTestHost.AnalyzeAsync(Sources + "\nint M(Maybe<int> m) { if (!m.IsSome) { return m.GetValue(); } return 0; }");

        Assert.Equal(["WYZ0201"], ids);
    }

    [Fact]
    internal async Task A_check_of_another_maybe_does_not_guard_the_access()
    {
        var ids = await AnalyzerTestHost.AnalyzeAsync(Sources + "\nint M(Maybe<int> a, Maybe<int> b) { if (a.IsSome) { return b.GetValue(); } return 0; }");

        Assert.Equal(["WYZ0201"], ids);
    }

    [Fact]
    internal async Task A_check_that_does_not_exit_does_not_guard_the_access()
    {
        var ids = await AnalyzerTestHost.AnalyzeAsync(Sources + "\nint M(Maybe<int> m) { if (m.IsNone) { Values.Clear(); } return m.GetValue(); }");

        Assert.Equal(["WYZ0201"], ids);
    }

    [Fact]
    internal async Task An_or_on_IsSome_does_not_guard_the_access()
    {
        var ids = await AnalyzerTestHost.AnalyzeAsync(Sources + "\nint M(Maybe<int> m, bool other) { if (m.IsSome || other) { return m.GetValue(); " +
            "} return 0; }");

        Assert.Equal(["WYZ0201"], ids);
    }

    [Fact]
    internal async Task GetValue_inside_a_lambda_without_a_check_is_reported()
    {
        var ids = await AnalyzerTestHost.AnalyzeAsync(Sources + "\nFunc<Maybe<int>, int> M() => m => m.GetValue();");

        Assert.Equal(["WYZ0201"], ids);
    }

    [Fact]
    internal async Task GetValue_inside_a_lambda_with_its_own_check_is_not_reported()
    {
        var ids = await AnalyzerTestHost.AnalyzeAsync(Sources + "\nFunc<Maybe<int>, int> M() => m => m.IsSome ? m.GetValue() : 0;");

        Assert.Empty(ids);
    }

    [Fact]
    internal async Task GetValue_inside_a_lambda_in_a_checked_branch_is_not_reported()
    {
        var ids = await AnalyzerTestHost.AnalyzeAsync(Sources + "\nFunc<int> M(Maybe<int> m) { if (m.IsSome) { return () => m.GetValue(); } return () => 0; }");

        Assert.Empty(ids);
    }

    [Fact]
    internal async Task GetValue_inside_a_local_function_without_a_check_is_reported()
    {
        var ids = await AnalyzerTestHost.AnalyzeAsync(Sources + "\nint M(Maybe<int> m) { int Read() => m.GetValue(); return Read(); }");

        Assert.Equal(["WYZ0201"], ids);
    }

    [Fact]
    internal async Task A_check_outside_a_local_function_does_not_guard_the_access_inside_it()
    {
        var ids = await AnalyzerTestHost.AnalyzeAsync(Sources + "\nint M(Maybe<int> m) { int Read() => m.GetValue(); if (m.IsNone) { return 0; } " +
            "return Read(); }");

        Assert.Equal(["WYZ0201"], ids);
    }

    [Fact]
    internal async Task GetValue_inside_a_local_function_with_its_own_check_is_not_reported()
    {
        var ids = await AnalyzerTestHost.AnalyzeAsync(Sources + "\nint M(Maybe<int> m) { int Read() { if (m.IsNone) { return 0; } return " +
            "m.GetValue(); } return Read(); }");

        Assert.Empty(ids);
    }

    [Fact]
    internal async Task A_check_in_one_method_does_not_guard_another_method()
    {
        var ids = await AnalyzerTestHost.AnalyzeAsync(Sources + "\nint A(Maybe<int> m) { if (m.IsNone) { return 0; } return 1; }\nint B(Maybe<int> " +
            "m) => m.GetValue();");

        Assert.Equal(["WYZ0201"], ids);
    }

    [Fact]
    internal async Task A_default_maybe_is_not_reported()
    {
        var ids = await AnalyzerTestHost.AnalyzeAsync(Sources + "\nMaybe<int> M() => default;\nMaybe<int> N() => default(Maybe<int>);\nMaybe<int> " +
            "O() => new Maybe<int>();");

        Assert.Empty(ids);
    }

    [Fact]
    internal async Task An_unused_maybe_is_not_reported()
    {
        var ids = await AnalyzerTestHost.AnalyzeAsync(Sources + "\nvoid M() { Compute(); }");

        Assert.Empty(ids);
    }

    [Fact]
    internal async Task ValueOr_TryGetValue_and_Match_are_not_reported()
    {
        var ids = await AnalyzerTestHost.AnalyzeAsync(
            Sources + "\nint M(Maybe<int> m) { m.TryGetValue(out var v); return m.ValueOr(0) + m.Match(x => x, () => 0) + v; }");

        Assert.Empty(ids);
    }

    [Fact]
    internal async Task A_method_with_the_same_name_on_another_type_is_not_reported()
    {
        var ids = await AnalyzerTestHost.AnalyzeAsync(
            """
            sealed class Other { public int GetValue() => 1; }
            int M(Other other) => other.GetValue();
            """);

        Assert.Empty(ids);
    }
}
