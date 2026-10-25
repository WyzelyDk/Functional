using Wyzely.Functional.ResultAnalyzers;

namespace Wyzely.Functional.ResultAnalyzersTests;

// A diagnostic can be stale, or made for code that has been edited since, so a code fix checks the code again and offers nothing that it cannot
// write. These tests ask the providers for a fix of a diagnostic that is made up and does not fit the code, and pin the forms that need more than
// the main tests: a message that has to stay lazy, a condition that has to be negated with parentheses, and the shapes of a guard clause.
public sealed class CodeFixEdgeCaseTests
{
    private const string Sources = """
        static Result<int> Twice(int x) => Result.Success(x * 2);
        static bool Big(int x) => x > 5;
        static bool Ready() => true;
        static string Describe() => "described";
        static readonly Error Rejected = Error.Create("Rejected.");
        Result<int> Field = Result.Success(2);
        """;

    private static readonly SuggestionsCodeFixProvider Suggestions = new();
    private static readonly ResultSuggestionsAnalyzer SuggestionsAnalyzer = new();
    private static readonly UncheckedAccessCodeFixProvider Guards = new();

    [Theory]
    [InlineData("WYZ0103", "Result<int> M(Result<int> r) => r.Bind(Twice);", "r.Bind(Twice)")]
    [InlineData("WYZ0103", "Result<int> M(Result<int> r) => r.Bind(x => Twice(x));", "r.Bind(x => Twice(x))")]
    [InlineData("WYZ0103", "Result<int> M(Result<int> r) => r.Bind(x => { var y = x; return Twice(y); });", "r.Bind(x => { var y = x; return Twice(y); })")]
    [InlineData("WYZ0104", "Result<int> M(Result<int> r) => r.Map(x => x + 1);", "r.Map(x => x + 1)")]
    [InlineData("WYZ0105", "Result<int> M(Result<int> r) => r.Ensure(Big, Rejected);", "r.Ensure(Big, Rejected)")]
    [InlineData("WYZ0105", "Result<int> M(Result<int> r) => r.Ensure(x => Big(x), Rejected);", "r.Ensure(x => Big(x), Rejected)")]
    [InlineData("WYZ0106", "Result<int> M(Result<int> r) => r.Bind(Twice);", "r.Bind(Twice)")]
    [InlineData("WYZ0106", "Result<int> M(Result<int> r) => r.Bind(x => Twice(x));", "r.Bind(x => Twice(x))")]
    [InlineData("WYZ0106", "Result<int> M(Result<int?> r) => r.Bind(x => x is null ? Twice(1) : Twice(2));", "r.Bind(x => x is null ? Twice(1) : Twice(2))")]
    [InlineData(
        "WYZ0106",
        "Result<int> M(Result<int?> r) => r.Bind(x => x is null ? Result.Failure<int>() : Twice(2));",
        "r.Bind(x => x is null ? Result.Failure<int>() : Twice(2))")]
    [InlineData("WYZ0107", "Result<int> M(Result<int> r) => r.Bind(Twice);", "r.Bind(Twice)")]
    [InlineData("WYZ0107", "Result<int> M(Result<int> r) => r.Bind(x => Twice(x));", "r.Bind(x => Twice(x))")]
    [InlineData("WYZ0108", "Result<int> M() => Result.Failure(Rejected);", "Result.Failure(Rejected)")]
    [InlineData("WYZ0108", "Result<int> M() => Result.Failure<int>();", "Result.Failure<int>()")]
    [InlineData("WYZ0108", "Result<int> M() => Result.Failure<int>(Rejected);", "Result.Failure<int>(Rejected)")]
    [InlineData("WYZ0109", "Result M(bool ok) => ok ? Result.Success() : Result.Success();", "ok ? Result.Success() : Result.Success()")]
    [InlineData("WYZ0109", "Result M(bool ok) => ok ? Result.Success() : Result.Failure();", "ok ? Result.Success() : Result.Failure()")]
    [InlineData("WYZ0109", "Result M(bool ok) => ok ? Result.Success() : Result.Failure(Rejected);", "ok ? Result.Success() : Result.Failure(Rejected)")]
    [InlineData("WYZ0110", "Result<int> M(Result<string> r) => r.Bind(Parse);", "r.Bind(Parse)")]
    [InlineData("WYZ0110", "Result<int> M(Result<string> r) => r.Bind(s => Twice(1));", "r.Bind(s => Twice(1))")]
    [InlineData("WYZ0110", "Result<int> M(Result<string> r) => r.Bind(s => Result.Try());", "r.Bind(s => Result.Try())")]
    [InlineData("WYZ0110", "Result<int> M(Result<string> r) => r.Bind(s => Result.Try(Parse));", "r.Bind(s => Result.Try(Parse))")]
    [InlineData("WYZ0112", "Result<int> M(Result<int> r) => r.Bind(Twice);", "r.Bind(Twice)")]
    [InlineData("WYZ0112", "Result<int> M(Result<int> r) => r.Bind(x => x);", "r.Bind(x => x)")]
    [InlineData("WYZ0113", "Result<int> M(Result<int> r) => r.Map(x => x + 1);", "r.Map(x => x + 1)")]
    [InlineData("WYZ0113", "Result<int> M(Result<int> r) => r.Map(Big);", "r.Map(Big)")]
    [InlineData("WYZ0114", "int M(Result<int> r) => r.Match(v => v);", "r.Match(v => v)")]
    [InlineData("WYZ0114", "int M(Result<int> r) => r.Match(onSuccess: v => v, onFailure: _ => 0);", "r.Match(onSuccess: v => v, onFailure: _ => 0)")]
    [InlineData("WYZ0114", "int M(Result<int> r) => r.Match(v => v, Fallback);", "r.Match(v => v, Fallback)")]
    [InlineData("WYZ0114", "int M(Result<int> r) => r.Match(v => v, _ => { Log(); return 0; });", "r.Match(v => v, _ => { Log(); return 0; })")]
    [InlineData("WYZ0115", "Result<int> M(Result<int> r) => Map(r, x => x);", "Map(r, x => x)")]
    [InlineData("WYZ0116", "Result M() => Result.Failure(Rejected);", "Result.Failure(Rejected)")]
    [InlineData("WYZ0116", "Result M() => Result.Failure(Error.Create(\"a\", ErrorKind.NotFound));", "Result.Failure(Error.Create(\"a\", ErrorKind.NotFound))")]
    [InlineData("WYZ0116", "Result M() => Result.Failure(first, second);", "Result.Failure(first, second)")]
    [InlineData("WYZ0117", "Result<int> M(Result<Result<int>> r) => Bind(r, x => x);", "Bind(r, x => x)")]
    [InlineData("WYZ0105", "Result<int> M(Result<int> r) => r.Ensure(x => Big(x), Rejected);", "Result<int> M")]
    [InlineData("WYZ0299", "Result<int> M(Result<int> r) => r.Ensure(x => !Big(x), Rejected);", "r.Ensure(x => !Big(x), Rejected)")]
    internal async Task No_fix_is_offered_for_a_suggestion_that_does_not_fit_the_code(string id, string method, string marker)
    {
        var offered = await CodeFixTestHost.OfferedFixesAsync(Sources + "\n" + method, Suggestions, id, marker);

        Assert.Equal(0, offered);
    }

    [Theory]
    [InlineData("WYZ0101", "Result M(Result r) => r;", "Result M")]
    [InlineData("WYZ0102", "int M(Result<int> r) => r.GetValue();", "int M")]
    internal async Task No_fix_is_offered_for_a_diagnostic_that_is_not_on_a_statement_or_a_call(string id, string method, string marker)
    {
        var unused = await CodeFixTestHost.OfferedFixesAsync(method, new UnusedResultCodeFixProvider(), id, marker);
        var guard = await CodeFixTestHost.OfferedFixesAsync(method, Guards, id, marker);

        Assert.Multiple(
            () => Assert.Equal(0, id == "WYZ0101" ? unused : 0),
            () => Assert.Equal(0, id == "WYZ0102" ? guard : 0));
    }

    [Theory]
    [InlineData(
        "Result<string> M(int id) => Result.Failure<string>(Wyzely.Functional.Error.Create($\"Order {id} not found.\"));",
        "$\"Order {id} not found.\"")]
    [InlineData("Error M(int id) => Wyzely.Functional.Error.Create($\"Order {id} not found.\");", "$\"Order {id} not found.\"")]
    [InlineData("Error M() => Error.Create(\"Order not found.\");", "\"Order not found.\"")]
    internal async Task No_fix_is_offered_for_a_message_that_is_not_interpolated_or_whose_error_is_not_written_with_its_short_name(string method, string marker)
    {
        var offered = await CodeFixTestHost.OfferedFixesAsync(method, new InterpolatedMessageCodeFixProvider(), "WYZ0111", marker);

        Assert.Equal(0, offered);
    }

    [Theory]
    [InlineData(
        "Error M(Order order) => Error.Create($\"Of {order.Customer.Name}.\");",
        "Error.Create(\"Of {Name}.\").WithDetail(\"Name\", order.Customer.Name)")]
    [InlineData("int Id => 1; Error M() => Error.Create($\"Order {this.Id}.\");", "Error.Create(\"Order {Id}.\").WithDetail(\"Id\", this.Id)")]
    internal async Task The_values_of_a_message_may_be_a_member_of_a_member_or_a_member_of_this(string method, string expected)
    {
        const string types = "record Customer(string Name); record Order(Customer Customer);";

        var outcome = await CodeFixTestHost.FixAsync(types + "\n" + method, new InterpolatedMessageCodeFixProvider(), "WYZ0111", new ResultMessageAnalyzer());

        Assert.Multiple(
            () => Assert.NotNull(outcome.FixedSource),
            () => Assert.Contains(expected, outcome.FixedSource, StringComparison.Ordinal),
            () => Assert.DoesNotContain("WYZ0111", outcome.RemainingIds));
    }

    [Theory]
    [InlineData("Error M(Order order) => Error.Create($\"Order {GetOrder().Id}.\");")]
    [InlineData("Error M(Order order) => Error.Create($\"Order {order?.Id}.\");")]
    [InlineData("Error M(Order order) => Error.Create($\"Order {order.Get<int>}.\");")]
    [InlineData("unsafe Error M(Order* order) => Error.Create($\"Order {order->Id}.\");")]
    internal async Task No_fix_is_offered_for_a_value_that_is_not_a_plain_member_access(string method)
    {
        var start = method.IndexOf("$\"", StringComparison.Ordinal);
        var marker = method[start..(method.LastIndexOf("\")", StringComparison.Ordinal) + 1)];

        var offered = await CodeFixTestHost.OfferedFixesAsync(method, new InterpolatedMessageCodeFixProvider(), "WYZ0111", marker);

        Assert.Equal(0, offered);
    }

    [Fact]
    internal async Task A_message_that_has_to_stay_lazy_is_created_with_Error_Create_inside_the_function()
    {
        const string method = "Result<string> M(Result<string?> r) => r.Bind(x => x is null ? Result.Failure<string>(Describe()) : Result.Success(x));";

        var outcome = await CodeFixTestHost.FixAsync(Sources + "\n" + method, Suggestions, "WYZ0106", SuggestionsAnalyzer);

        Assert.Multiple(
            () => Assert.NotNull(outcome.FixedSource),
            () => Assert.Contains("r.EnsureNotNull(() => Error.Create(Describe()))", outcome.FixedSource, StringComparison.Ordinal),
            () => Assert.DoesNotContain("WYZ0106", outcome.RemainingIds));
    }

    [Fact]
    internal async Task A_condition_that_is_not_a_simple_expression_is_negated_with_parentheses()
    {
        const string method = "Result<int> M(Result<int> r) => r.Bind(x => Ready() && Big(x) ? Result.Success(x) : Twice(x));";

        var outcome = await CodeFixTestHost.FixAsync(Sources + "\n" + method, Suggestions, "WYZ0103", SuggestionsAnalyzer);

        Assert.Multiple(
            () => Assert.NotNull(outcome.FixedSource),
            () => Assert.Contains("r.BindIf(x => !(Ready() && Big(x)), x => Twice(x))", outcome.FixedSource, StringComparison.Ordinal),
            () => Assert.DoesNotContain("WYZ0103", outcome.RemainingIds));
    }

    [Fact]
    internal async Task A_call_that_is_not_written_with_a_receiver_is_renamed_in_place()
    {
        const string method = "Result<int> M(Result<int> r) => Bind(x => Ready() ? Twice(x) : Result.Success(x));";

        var offered = await CodeFixTestHost.OfferedFixesAsync(
            Sources + "\n" + method,
            Suggestions,
            "WYZ0103",
            "Bind(x => Ready() ? Twice(x) : Result.Success(x))");

        Assert.Equal(1, offered);
    }

    [Fact]
    internal async Task A_diagnostic_without_the_properties_of_the_analyzer_gets_the_plain_form()
    {
        const string method = "Result<int> M(Result<int> r) => r.Bind(x => Ready() ? Twice(x) : Result.Success(x));";

        var offered = await CodeFixTestHost.OfferedFixesAsync(
            Sources + "\n" + method,
            Suggestions,
            "WYZ0103",
            "r.Bind(x => Ready() ? Twice(x) : Result.Success(x))");

        Assert.Equal(1, offered);
    }

    [Theory]
    [InlineData(
        "Result<int> M() { var v = this.Field.GetValue(); return Result.Success(v); }",
        "if (this.Field.IsFailure)",
        "return this.Field;")]
    [InlineData(
        "Result<string> M(Result<int> r) { var v = r.GetValue().ToString(); return Result.Success(v); }",
        "if (r.IsFailure)",
        "return r.ConvertFailure<string>();")]
    [InlineData(
        "Result<int> M(Result<int[]> r) { var first = r.GetValue()[0]; return Result.Success(first); }",
        "if (r.IsFailure)",
        "return r.ConvertFailure<int>();")]
    [InlineData(
        "Result<int> M(Result<int> r) { var v = r.GetValue() + 1; return Result.Success(v); }",
        "if (r.IsFailure)",
        "return r;")]
    [InlineData(
        "async ValueTask<Result<string>> M(Result<int> r) { var v = r.GetValue(); await Task.Yield(); return Result.Success(v.ToString()); }",
        "if (r.IsFailure)",
        "return r.ConvertFailure<string>();")]
    internal async Task The_guard_clause_is_written_for_a_member_a_chain_an_index_a_sum_and_a_value_task(string method, string guard, string returned)
    {
        var outcome = await CodeFixTestHost.FixAsync(Sources + "\n" + method, Guards, "WYZ0102");

        Assert.Multiple(
            () => Assert.NotNull(outcome.FixedSource),
            () => Assert.Contains(guard, outcome.FixedSource, StringComparison.Ordinal),
            () => Assert.Contains(returned, outcome.FixedSource, StringComparison.Ordinal),
            () => Assert.DoesNotContain("WYZ0102", outcome.RemainingIds));
    }

    [Theory]
    [InlineData("Result<bool> M(Result<int> r, bool other) { var b = r.GetValue() > 0 && other; return Result.Success(b); }")]
    [InlineData("async Task M(Result<int> r) { var v = r.GetValue(); await Task.Yield(); }")]
    [InlineData("static Result<int> Source; static int Value = Source.GetValue();")]
    internal async Task No_guard_clause_is_offered_after_a_short_circuit_in_a_method_without_a_result_or_outside_a_statement(string method)
    {
        var outcome = await CodeFixTestHost.FixAsync(Sources + "\n" + method, Guards, "WYZ0102");

        Assert.Null(outcome.FixedSource);
    }
}
