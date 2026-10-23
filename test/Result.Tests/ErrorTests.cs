namespace Wyzely.Functional.ResultTests;

public sealed class ErrorTests
{
    [Fact]
    internal void Create_returns_a_failure_error_with_the_message_and_no_code()
    {
        var error = Error.Create("Something broke.");

        Assert.Multiple(
            () => Assert.Equal("Something broke.", error.Message),
            () => Assert.Equal("Something broke.", error.Template),
            () => Assert.Equal(ErrorKind.Failure, error.Kind),
            () => Assert.Null(error.Code),
            () => Assert.False(error.IsTransient),
            () => Assert.Null(error.Exception),
            () => Assert.Empty(error.Details),
            () => Assert.Empty(error.Context),
            () => Assert.Empty(error.Children),
            () => Assert.False(error.IsAggregate));
    }

    [Fact]
    internal void Create_uses_the_given_kind()
    {
        var error = Error.Create("Missing.", ErrorKind.NotFound);

        Assert.Equal(ErrorKind.NotFound, error.Kind);
    }

    [Fact]
    internal void Create_throws_when_the_message_is_null()
    {
        Assert.Throws<ArgumentNullException>(() => Error.Create(null!));
    }

    [Fact]
    internal void Create_throws_when_the_message_is_whitespace()
    {
        Assert.Throws<ArgumentException>(() => Error.Create(" "));
    }

    [Fact]
    internal void Named_factories_set_the_matching_kind()
    {
        Assert.Multiple(
            () => Assert.Equal(ErrorKind.Unexpected, Error.Unexpected("x").Kind),
            () => Assert.Equal(ErrorKind.Validation, Error.Validation("x").Kind),
            () => Assert.Equal(ErrorKind.NotFound, Error.NotFound("x").Kind),
            () => Assert.Equal(ErrorKind.Conflict, Error.Conflict("x").Kind),
            () => Assert.Equal(ErrorKind.Unauthorized, Error.Unauthorized("x").Kind),
            () => Assert.Equal(ErrorKind.Forbidden, Error.Forbidden("x").Kind),
            () => Assert.Equal(ErrorKind.Unavailable, Error.Unavailable("x").Kind),
            () => Assert.Equal(ErrorKind.Timeout, Error.Timeout("x").Kind));
    }

    [Fact]
    internal void Unavailable_and_timeout_errors_are_transient()
    {
        Assert.Multiple(
            () => Assert.True(Error.Unavailable("x").IsTransient),
            () => Assert.True(Error.Timeout("x").IsTransient),
            () => Assert.False(Error.NotFound("x").IsTransient));
    }

    [Fact]
    internal void Cancelled_has_a_default_message_and_the_cancelled_kind()
    {
        var error = Error.Cancelled();

        Assert.Multiple(
            () => Assert.Equal(ErrorKind.Cancelled, error.Kind),
            () => Assert.Equal("The operation was cancelled.", error.Message));
    }

    [Fact]
    internal void Cancelled_uses_the_given_message()
    {
        Assert.Equal("Stopped by the user.", Error.Cancelled("Stopped by the user.").Message);
    }

    [Fact]
    internal void Message_replaces_placeholders_with_the_values_of_the_details()
    {
        var error = Error.Create("Upload to {Path} failed after {Attempts} attempts.")
            .WithDetail("Path", "a/b.json")
            .WithDetail("Attempts", 3);

        Assert.Equal("Upload to a/b.json failed after 3 attempts.", error.Message);
    }

    [Fact]
    internal void Message_keeps_a_placeholder_that_has_no_detail()
    {
        var error = Error.Create("Upload to {Path} failed.");

        Assert.Equal("Upload to {Path} failed.", error.Message);
    }

    [Fact]
    internal void Message_turns_doubled_braces_into_single_braces()
    {
        var error = Error.Create("Expected {{braces}} around {Name}.").WithDetail("Name", "x");

        Assert.Equal("Expected {braces} around x.", error.Message);
    }

    [Fact]
    internal void Message_keeps_an_unclosed_brace_as_it_is()
    {
        var error = Error.Create("Broken {template").WithDetail("template", "x");

        Assert.Equal("Broken {template", error.Message);
    }

    [Fact]
    internal void Message_renders_a_null_detail_as_null()
    {
        var error = Error.Create("Value was {Value}.").WithDetail("Value", null);

        Assert.Equal("Value was null.", error.Message);
    }

    [Fact]
    internal void Message_uses_the_first_detail_when_a_name_occurs_twice()
    {
        var error = Error.Create("{Name}").WithDetail("Name", "first").WithDetail("Name", "second");

        Assert.Equal("first", error.Message);
    }

    [Fact]
    internal void RedactedMessage_hides_sensitive_details_and_keeps_the_others()
    {
        var error = Error.Create("Login of {User} with {Password} failed.")
            .WithDetail("User", "anna")
            .WithDetail("Password", "hunter2", isSensitive: true);

        Assert.Multiple(
            () => Assert.Equal("Login of anna with hunter2 failed.", error.Message),
            () => Assert.Equal("Login of anna with *** failed.", error.RedactedMessage));
    }

    [Fact]
    internal void WithCode_returns_a_copy_with_the_code_and_leaves_the_original_unchanged()
    {
        var original = Error.Create("x");

        var coded = original.WithCode("E001");

        Assert.Multiple(
            () => Assert.Equal("E001", coded.Code),
            () => Assert.Null(original.Code));
    }

    [Fact]
    internal void WithKind_returns_a_copy_with_the_kind()
    {
        var error = Error.Create("x").WithKind(ErrorKind.Conflict);

        Assert.Equal(ErrorKind.Conflict, error.Kind);
    }

    [Fact]
    internal void WithTransient_marks_the_error_as_transient_by_default()
    {
        var error = Error.Create("x");

        Assert.Multiple(
            () => Assert.True(error.WithTransient().IsTransient),
            () => Assert.False(error.WithTransient().WithTransient(false).IsTransient));
    }

    [Fact]
    internal void WithDetail_appends_details_in_order()
    {
        var error = Error.Create("x").WithDetail("A", 1).WithDetail("B", 2, isSensitive: true);

        Assert.Equal([new ErrorDetail("A", 1), new ErrorDetail("B", 2, isSensitive: true)], error.Details);
    }

    [Fact]
    internal void WithDetail_throws_when_the_name_is_null()
    {
        Assert.Throws<ArgumentNullException>(() => Error.Create("x").WithDetail(null!, 1));
    }

    [Fact]
    internal void WithException_sets_the_cause()
    {
        var cause = new InvalidOperationException("boom");

        var error = Error.Create("x").WithException(cause);

        Assert.Same(cause, error.Exception);
    }

    [Fact]
    internal void WithException_throws_when_the_exception_is_null()
    {
        Assert.Throws<ArgumentNullException>(() => Error.Create("x").WithException(null!));
    }

    [Fact]
    internal void WithContext_adds_frames_with_the_latest_one_outermost()
    {
        var error = Error.Create("Upload failed.").WithContext("Checkout").WithContext("Import");

        Assert.Equal(["Import", "Checkout"], error.Context.Select(static frame => frame.Message));
    }

    [Fact]
    internal void WithContext_throws_when_the_context_is_whitespace()
    {
        Assert.Throws<ArgumentException>(() => Error.Create("x").WithContext(" "));
    }

    [Fact]
    internal void ToString_prefixes_the_context_frames_outermost_first()
    {
        var error = Error.Create("Upload failed.").WithContext("Checkout").WithContext("Import");

        Assert.Equal("Import: Checkout: Upload failed.", error.ToString());
    }

    [Fact]
    internal void ToString_without_context_is_the_message()
    {
        Assert.Equal("Upload failed.", Error.Create("Upload failed.").ToString());
    }

    [Fact]
    internal void ToRedactedString_uses_the_redacted_message()
    {
        var error = Error.Create("Token {Token}.").WithDetail("Token", "abc", isSensitive: true).WithContext("Auth");

        Assert.Equal("Auth: Token ***.", error.ToRedactedString());
    }

    [Fact]
    internal void FromException_uses_the_message_of_the_exception_and_keeps_it_as_the_cause()
    {
        var exception = new InvalidOperationException("boom");

        var error = Error.FromException(exception);

        Assert.Multiple(
            () => Assert.Equal("boom", error.Message),
            () => Assert.Same(exception, error.Exception),
            () => Assert.Equal(ErrorKind.Unexpected, error.Kind));
    }

    [Fact]
    internal void FromException_keeps_braces_in_the_message_of_the_exception_as_text()
    {
        var error = Error.FromException(new InvalidOperationException("Bad {value} and {{escaped}}"));

        Assert.Equal("Bad {value} and {{escaped}}", error.Message);
    }

    [Fact]
    internal void FromException_falls_back_to_the_type_name_when_the_message_is_empty()
    {
        var error = Error.FromException(new EmptyMessageException());

        Assert.Equal("EmptyMessageException", error.Message);
    }

    [Fact]
    internal void FromException_uses_a_given_message_instead_of_the_one_of_the_exception()
    {
        var error = Error.FromException(new InvalidOperationException("boom"), "Could not save.");

        Assert.Equal("Could not save.", error.Message);
    }

    [Fact]
    internal void FromException_maps_a_cancellation_to_the_cancelled_kind()
    {
        var error = Error.FromException(new OperationCanceledException());

        Assert.Equal(ErrorKind.Cancelled, error.Kind);
    }

    [Fact]
    internal void FromException_maps_a_timeout_to_a_transient_timeout()
    {
        var error = Error.FromException(new TimeoutException());

        Assert.Multiple(
            () => Assert.Equal(ErrorKind.Timeout, error.Kind),
            () => Assert.True(error.IsTransient));
    }

    [Fact]
    internal void FromException_throws_when_the_exception_is_null()
    {
        Assert.Throws<ArgumentNullException>(() => Error.FromException(null!));
    }

    [Fact]
    internal void Aggregate_returns_the_error_itself_when_given_a_single_error()
    {
        var error = Error.Create("x");

        var aggregate = Error.Aggregate([error]);

        Assert.Same(error, aggregate);
    }

    [Fact]
    internal void Aggregate_combines_several_errors_into_one()
    {
        var first = Error.Create("a");
        var second = Error.Create("b");

        var aggregate = Error.Aggregate([first, second]);

        Assert.Multiple(
            () => Assert.True(aggregate.IsAggregate),
            () => Assert.Equal([first, second], aggregate.Children));
    }

    [Fact]
    internal void Aggregate_flattens_nested_aggregates()
    {
        var a = Error.Create("a");
        var b = Error.Create("b");
        var c = Error.Create("c");
        var inner = Error.Aggregate([a, b]);

        var aggregate = Error.Aggregate([inner, c]);

        Assert.Equal([a, b, c], aggregate.Children);
    }

    [Fact]
    internal void Aggregate_throws_when_there_are_no_errors()
    {
        Assert.Throws<ArgumentException>(() => Error.Aggregate([]));
    }

    [Fact]
    internal void Aggregate_throws_when_the_sequence_is_null()
    {
        Assert.Throws<ArgumentNullException>(() => Error.Aggregate(null!));
    }

    [Fact]
    internal void Aggregate_throws_when_a_child_is_null()
    {
        Assert.Throws<ArgumentException>(() => Error.Aggregate([Error.Create("a"), null!]));
    }

    [Fact]
    internal void Aggregate_message_joins_the_descriptions_of_the_children()
    {
        var aggregate = Error.Aggregate([Error.Create("a").WithContext("Checkout"), Error.Create("b")]);

        Assert.Multiple(
            () => Assert.Equal("Checkout: a, b", aggregate.Message),
            () => Assert.Equal("Checkout: a, b", aggregate.ToString()));
    }

    [Fact]
    internal void Aggregate_redacted_message_joins_the_redacted_descriptions()
    {
        var secret = Error.Create("Token {Token}").WithDetail("Token", "abc", isSensitive: true);

        var aggregate = Error.Aggregate([secret, Error.Create("b")]);

        Assert.Equal("Token ***, b", aggregate.RedactedMessage);
    }

    [Fact]
    internal void Aggregate_takes_the_kind_the_children_share_and_failure_otherwise()
    {
        var shared = Error.Aggregate([Error.NotFound("a"), Error.NotFound("b")]);
        var mixed = Error.Aggregate([Error.NotFound("a"), Error.Conflict("b")]);

        Assert.Multiple(
            () => Assert.Equal(ErrorKind.NotFound, shared.Kind),
            () => Assert.Equal(ErrorKind.Failure, mixed.Kind));
    }

    [Fact]
    internal void Aggregate_is_transient_only_when_every_child_is()
    {
        var allTransient = Error.Aggregate([Error.Timeout("a"), Error.Unavailable("b")]);
        var someTransient = Error.Aggregate([Error.Timeout("a"), Error.NotFound("b")]);

        Assert.Multiple(
            () => Assert.True(allTransient.IsTransient),
            () => Assert.False(someTransient.IsTransient));
    }

    [Fact]
    internal void WithContext_on_an_aggregate_applies_to_every_child()
    {
        var aggregate = Error.Aggregate([Error.Create("a"), Error.Create("b")]).WithContext("Checkout");

        Assert.Equal(["Checkout: a", "Checkout: b"], aggregate.Children.Select(child => child.ToString()));
    }

    [Fact]
    internal void Flatten_returns_the_error_itself_when_it_is_not_an_aggregate()
    {
        var error = Error.Create("a");

        Assert.Equal([error], error.Flatten());
    }

    [Fact]
    internal void Flatten_returns_the_children_of_an_aggregate()
    {
        var a = Error.Create("a");
        var b = Error.Create("b");

        Assert.Equal([a, b], Error.Aggregate([a, b]).Flatten());
    }

    [Fact]
    internal void Equality_is_structural()
    {
        var first = Error.Create("Upload to {Path}.").WithDetail("Path", "a").WithCode("E1").WithContext("Checkout");
        var second = Error.Create("Upload to {Path}.").WithDetail("Path", "a").WithCode("E1").WithContext("Checkout");

        Assert.Multiple(
            () => Assert.True(first.Equals(second)),
            () => Assert.True(first == second),
            () => Assert.False(first != second),
            () => Assert.Equal(first.GetHashCode(), second.GetHashCode()),
            () => Assert.True(first.Equals((object)second)),
            () => Assert.False(first.Equals((object?)null)),
            () => Assert.False(first.Equals("text")));
    }

    [Fact]
    internal void Equality_distinguishes_every_part_of_an_error()
    {
        var baseline = Error.Create("x");

        Assert.Multiple(
            () => Assert.NotEqual(baseline, Error.Create("y")),
            () => Assert.NotEqual(baseline, baseline.WithCode("E1")),
            () => Assert.NotEqual(baseline, baseline.WithKind(ErrorKind.Conflict)),
            () => Assert.NotEqual(baseline, baseline.WithTransient()),
            () => Assert.NotEqual(baseline, baseline.WithDetail("A", 1)),
            () => Assert.NotEqual(baseline, baseline.WithContext("Checkout")),
            () => Assert.NotEqual(baseline, baseline.WithException(new InvalidOperationException())),
            () => Assert.NotEqual(Error.Aggregate([baseline, Error.Create("z")]), Error.Aggregate([baseline, Error.Create("w")])));
    }

    [Fact]
    internal void Equality_treats_null_as_not_equal()
    {
        Error? none = null;
        Error? other = null;
        var error = Error.Create("x");

        Assert.Multiple(
            () => Assert.False(error == none),
            () => Assert.True(error != none),
            () => Assert.True(none == other));
    }

    public sealed class EmptyMessageException : Exception
    {
        public override string Message => string.Empty;
    }
}
