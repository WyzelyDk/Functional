using Wyzely.Functional.MaybeSamples;

namespace Wyzely.Functional.SamplesTests;

// Each Maybe example prints labelled lines; these tests pin the lines that matter, so a sample cannot silently stop saying what its
// comments claim. One class, so that the examples that change the process-wide MaybeConfiguration never overlap with the others.
public sealed class MaybeSamplesTests
{
    [Fact]
    internal void BasicsExample_creates_reads_and_leaves_a_maybe()
    {
        var lines = Run(BasicsExample.Run);

        Assert.Multiple(
            () => Assert.Contains("Maybe.Some(\"Ada\"): Some(Ada)", lines),
            () => Assert.Contains("Maybe.None<string>(): None", lines),
            () => Assert.Contains("Maybe.From(null reference): None", lines),
            () => Assert.Contains("Maybe.From(non-null reference): Some(Ada)", lines),
            () => Assert.Contains("Maybe.From(int? with a value): Some(12)", lines),
            () => Assert.Contains("Maybe.SomeWhen(true, 3): Some(3)", lines),
            () => Assert.Contains("Maybe.SomeWhen(false, 3): None", lines),
            () => Assert.Contains("Maybe.NoneWhen(true, 3): None", lines),
            () => Assert.Contains("Maybe.NoneWhen(false, 3): Some(3)", lines),
            () => Assert.Contains("Implicit conversion of a value: Some(Ada)", lines),
            () => Assert.Contains("Implicit conversion of null: None", lines),
            () => Assert.Contains("default(Maybe<int>) is none: True", lines),
            () => Assert.Contains("Maybe.Some(null) throws: ArgumentNullException", lines),
            () => Assert.Contains("TryGetValue: Ada has 3 letters", lines),
            () => Assert.Contains("ValueOr on a some: Ada", lines),
            () => Assert.Contains("ValueOr on a none: nobody", lines),
            () => Assert.Contains("Match on a some: Hello, Ada", lines),
            () => Assert.Contains("Match on a none: Hello, stranger", lines),
            () => Assert.Contains("GetValue after an IsSome check: Ada", lines),
            () => Assert.Contains("GetValue on a none throws: InvalidOperationException", lines));
    }

    [Fact]
    internal void RailwayExample_runs_each_operation_on_the_some_or_the_none_track()
    {
        var lines = Run(RailwayExample.Run);

        Assert.Multiple(
            () => Assert.Contains("Map: Some(ADA)", lines),
            () => Assert.Contains("Bind, customer with an email: Some(ada@example.com)", lines),
            () => Assert.Contains("Bind, customer without an email: None", lines),
            () => Assert.Contains("Filter, active customer: Some(Ada)", lines),
            () => Assert.Contains("Filter, inactive customer: None", lines),
            () => Assert.Contains("Tap on a some ran: True", lines),
            () => Assert.Contains("Tap on a none ran: False", lines),
            () => Assert.Contains("TapNone on a none ran: True", lines),
            () => Assert.Contains("TapNone on a some ran: False", lines),
            () => Assert.Contains("Or, alternative used: Some(support@example.com)", lines),
            () => Assert.Contains("Or, alternative not needed: Some(ada@example.com)", lines),
            () => Assert.Contains("ValueOr(Func): no email on file", lines),
            () => Assert.Contains("Match, some: Send to ada@example.com", lines),
            () => Assert.Contains("Match, none: Nobody to mail", lines),
            () => Assert.Contains("Switch, some: found Ada", lines),
            () => Assert.Contains("Switch, none: nobody found", lines),
            () => Assert.Contains("Whole chain, customer 1: Mail sent to ada@example.com", lines),
            () => Assert.Contains("Whole chain, customer 2 (no email): Nobody to mail", lines),
            () => Assert.Contains("Whole chain, customer 3 (inactive): Nobody to mail", lines),
            () => Assert.Contains("Whole chain, customer 99 (unknown): Nobody to mail", lines));
    }

    [Fact]
    internal void AsyncExample_mixes_all_nine_combinations_under_one_name()
    {
        var lines = Run(AsyncExample.Run);

        Assert.Multiple(
            () => Assert.Contains("Maybe receiver, sync delegate: Some(2)", lines),
            () => Assert.Contains("Maybe receiver, Task delegate: Some(2)", lines),
            () => Assert.Contains("Maybe receiver, ValueTask delegate: Some(2)", lines),
            () => Assert.Contains("Task receiver, sync delegate: Some(2)", lines),
            () => Assert.Contains("Task receiver, Task delegate: Some(2)", lines),
            () => Assert.Contains("Task receiver, ValueTask delegate: Some(2)", lines),
            () => Assert.Contains("ValueTask receiver, sync delegate: Some(2)", lines),
            () => Assert.Contains("ValueTask receiver, Task delegate: Some(2)", lines),
            () => Assert.Contains("ValueTask receiver, ValueTask delegate: Some(2)", lines),
            () => Assert.Contains("Mixed chain, customer 1: Mail sent to ada@example.com", lines),
            () => Assert.Contains("Mixed chain, customer 2 (no email): Nobody to mail", lines),
            () => Assert.Contains("Mixed chain, customer 3 (inactive): Nobody to mail", lines),
            () => Assert.Contains("Mixed chain, customer 99 (unknown): Nobody to mail", lines),
            () => Assert.Contains("Async lambda: Some(42)", lines),
            () => Assert.Contains("A none skips the Task delegate: False", lines));
    }

    [Fact]
    internal void CollectionsExample_answers_the_maybe_there_is_one_questions()
    {
        var lines = Run(CollectionsExample.Run);

        Assert.Multiple(
            () => Assert.Contains("FirstOrNone(n > 10): Some(15)", lines),
            () => Assert.Contains("FirstOrNone(n > 100): None", lines),
            () => Assert.Contains("FirstOrNone() on an empty sequence: None", lines),
            () => Assert.Contains("LastOrNone(): Some(16)", lines),
            () => Assert.Contains("SingleOrNone(n > 15): Some(16)", lines),
            () => Assert.Contains("SingleOrNone(n > 10), two match: None", lines),
            () => Assert.Contains("SingleOrNone(n > 100), none match: None", lines),
            () => Assert.Contains("ElementAtOrNone(2): Some(15)", lines),
            () => Assert.Contains("ElementAtOrNone(10): None", lines),
            () => Assert.Contains("ElementAtOrNone(-1): None", lines),
            () => Assert.Contains("GetValueOrNone(\"tea\"): Some(350)", lines),
            () => Assert.Contains("GetValueOrNone(\"water\"): None", lines),
            () => Assert.Contains("Choose(): 1, 3", lines),
            () => Assert.Contains("Choose(selector), emails of all customers: ada@example.com, cleo@example.com", lines));
    }

    [Fact]
    internal void LinqExample_combines_maybes_with_query_syntax_zip_and_flatten()
    {
        var lines = Run(LinqExample.Run);

        Assert.Multiple(
            () => Assert.Contains("Query syntax, customer 1: Some(Ada <ada@example.com>)", lines),
            () => Assert.Contains("Query syntax, customer 2 (no email): None", lines),
            () => Assert.Contains("Query syntax, customer 3 (inactive): None", lines),
            () => Assert.Contains("Zip into a tuple: Some((Ada, 3))", lines),
            () => Assert.Contains("Zip with a selector: Some(12)", lines),
            () => Assert.Contains("Zip with a missing side: None", lines),
            () => Assert.Contains("Flatten: Some(Ada)", lines),
            () => Assert.Contains("Flatten of an inner none: None", lines),
            () => Assert.Contains("Flatten of an outer none: None", lines),
            () => Assert.Contains("Bind says the same without nesting: Some(Ada)", lines));
    }

    [Fact]
    internal void NullabilityExample_turns_nullable_properties_into_maybes()
    {
        var lines = Run(NullabilityExample.Run);

        Assert.Multiple(
            () => Assert.Contains("Map to a string? property, nickname set: Some(Countess)", lines),
            () => Assert.Contains("Map to a string? property, no nickname: None", lines),
            () => Assert.Contains("Map delegate that returns null becomes none: None", lines),
            () => Assert.Contains("Bind with Maybe.From on an int? property, age known: Some(36)", lines),
            () => Assert.Contains("Bind with Maybe.From on an int? property, age unknown: None", lines),
            () => Assert.Contains("Maybe.From(int? with a value): Some(12)", lines),
            () => Assert.Contains("Maybe.From(int? without a value): None", lines),
            () => Assert.Contains("ToNullable of a some: 12", lines),
            () => Assert.Contains("ToNullable of a none: null", lines),
            () => Assert.Contains("Implicit conversion of a string? that is null: None", lines),
            () => Assert.Contains("Implicit conversion of a string? that is set: Some(Ada)", lines));
    }

    [Fact]
    internal void EqualityExample_compares_orders_and_formats_maybes()
    {
        var lines = Run(EqualityExample.Run);

        Assert.Multiple(
            () => Assert.Contains("Some(5) == Some(5): True", lines),
            () => Assert.Contains("Some(5) == Some(6): False", lines),
            () => Assert.Contains("Some(5) == None: False", lines),
            () => Assert.Contains("None == default: True", lines),
            () => Assert.Contains("Equals(object): True", lines),
            () => Assert.Contains("None < Some(1): True", lines),
            () => Assert.Contains("Some(1) < Some(2): True", lines),
            () => Assert.Contains("Sorted: None, Some(1), Some(3)", lines),
            () => Assert.Contains("Distinct in a HashSet: 2", lines),
            () => Assert.Contains("Equal values have equal hash codes: True", lines),
            () => Assert.Contains("ToString of a some: Some(5)", lines),
            () => Assert.Contains("ToString of a none: None", lines),
            () => Assert.Contains("ToString of a default: None", lines),
            () => Assert.Contains("Interpolated into text: Customer: Some(Ada)", lines));
    }

    [Fact]
    internal void ConfigurationExample_changes_the_settings_and_restores_the_defaults()
    {
        var lines = Run(ConfigurationExample.Run);

        Assert.Multiple(
            () => Assert.Contains("Default some: Some(5)", lines),
            () => Assert.Contains("Default none: None", lines),
            () => Assert.Contains("Custom some: Just(5)", lines),
            () => Assert.Contains("Custom none: Nothing", lines),
            () => Assert.Contains("Custom message: There is nothing to read here.", lines),
            () => Assert.Contains("Redacted some: Some(***)", lines),
            () => Assert.Contains("Redacted none: None", lines),
            () => Assert.Contains("Custom redaction: Some([hidden])", lines),
            () => Assert.Contains("A format without {0} is rejected: ArgumentException", lines),
            () => Assert.Contains("A Configure that throws changes nothing: Some(5)", lines),
            () => Assert.Contains("ContinueOnCapturedContext false, continuations posted to the context: 0", lines),
            () => Assert.Contains("ContinueOnCapturedContext true, continuations posted to the context: more than 0", lines),
            () => Assert.Contains("After Reset: Some(5)", lines),
            () => Assert.Equal("Some(5)", Maybe.Some(5).ToString()));
    }

#if NET11_0_OR_GREATER
    [Fact]
    internal void UnionExample_switches_exhaustively_on_the_value_and_null()
    {
        var lines = Run(UnionExample.Run);

        Assert.Multiple(
            () => Assert.Contains("Switch on a some: found Ada", lines),
            () => Assert.Contains("Switch on a none: nobody", lines),
            () => Assert.Contains("Switch on a default maybe: nobody", lines),
            () => Assert.Contains("Implicit conversion from the case type: Some(5)", lines),
            () => Assert.Contains("Switch with a nested pattern, active customer: Ada can be mailed", lines),
            () => Assert.Contains("Switch with a nested pattern, inactive customer: Cleo is inactive", lines),
            () => Assert.Contains("Switch with a nested pattern, nobody: nobody", lines));
    }
#endif

    private static string[] Run(Action<TextWriter> example)
    {
        using var writer = new StringWriter();
        example(writer);

        return writer.ToString().Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries);
    }
}
