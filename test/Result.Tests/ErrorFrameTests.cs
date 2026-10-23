namespace Wyzely.Functional.ResultTests;

// A context frame is a message template with details of its own, like the message of an error: a value that a layer adds is structured and can be
// redacted, instead of being baked into text.
public sealed class ErrorFrameTests
{
    private static readonly Error Upload = Error.Create("Upload failed.");

    [Fact]
    internal void WithContext_with_a_detail_fills_the_placeholder_of_the_frame()
    {
        var frame = Upload.WithContext("Checkout of order {OrderId}", "OrderId", 7).Context[0];

        Assert.Multiple(
            () => Assert.Equal("Checkout of order {OrderId}", frame.Template),
            () => Assert.Equal("Checkout of order 7", frame.Message),
            () => Assert.Equal("Checkout of order 7", frame.ToString()),
            () => Assert.Equal(new ErrorDetail("OrderId", 7), Assert.Single(frame.Details)));
    }

    [Fact]
    internal void WithContext_with_several_details_fills_every_placeholder_and_keeps_the_order_of_the_details()
    {
        var frame = Upload.WithContext(
            "Checkout of {OrderId} for {Customer}",
            new ErrorDetail("OrderId", 7),
            new ErrorDetail("Customer", "Ada")).Context[0];

        Assert.Multiple(
            () => Assert.Equal("Checkout of 7 for Ada", frame.Message),
            () => Assert.Equal(["OrderId", "Customer"], frame.Details.Select(static detail => detail.Name)));
    }

    [Fact]
    internal void A_plain_frame_has_no_details_and_is_its_own_text()
    {
        var frame = Upload.WithContext("Checkout").Context[0];

        Assert.Multiple(
            () => Assert.Equal("Checkout", frame.Template),
            () => Assert.Equal("Checkout", frame.Message),
            () => Assert.Empty(frame.Details));
    }

    [Fact]
    internal void The_text_of_the_error_starts_with_the_rendered_frames_outermost_first()
    {
        var error = Upload.WithContext("Order {Id}", "Id", 1).WithContext("Import of {File}", "File", "a.csv");

        Assert.Equal("Import of a.csv: Order 1: Upload failed.", error.ToString());
    }

    [Fact]
    internal void A_sensitive_detail_of_a_frame_is_hidden_in_the_redacted_text_and_shown_in_the_plain_text()
    {
        var error = Upload.WithContext("Mail to {Email}", "Email", "ada@example.com", isSensitive: true);
        var frame = error.Context[0];

        Assert.Multiple(
            () => Assert.Equal("Mail to ada@example.com: Upload failed.", error.ToString()),
            () => Assert.Equal("Mail to ***: Upload failed.", error.ToRedactedString()),
            () => Assert.Equal("Mail to ada@example.com", frame.Message),
            () => Assert.Equal("Mail to ***", frame.RedactedMessage));
    }

    [Fact]
    internal void The_placeholders_of_a_frame_are_filled_from_its_own_details_and_never_from_the_details_of_the_error()
    {
        var error = Error.Create("Failed for {OrderId}.").WithDetail("OrderId", 1).WithContext("Order {OrderId}");

        Assert.Multiple(
            () => Assert.Equal("Order {OrderId}", error.Context[0].Message),
            () => Assert.Equal("Failed for 1.", error.Message));
    }

    [Fact]
    internal void Two_layers_can_use_the_same_name_for_different_values()
    {
        var error = Upload.WithContext("Inner {Id}", "Id", 1).WithContext("Outer {Id}", "Id", 2);

        Assert.Equal("Outer 2: Inner 1: Upload failed.", error.ToString());
    }

    [Fact]
    internal void A_placeholder_without_a_detail_is_kept_as_written_and_doubled_braces_become_single_ones()
    {
        var unmatched = Upload.WithContext("Order {Id} of {{literal}}").Context[0];

        Assert.Equal("Order {Id} of {literal}", unmatched.Message);
    }

    [Fact]
    internal void The_message_of_a_frame_is_rendered_once()
    {
        var frame = Upload.WithContext("Order {Id}", "Id", 1).Context[0];

        Assert.Same(frame.Message, frame.Message);
    }

    [Fact]
    internal void WithContext_adds_the_frame_to_every_child_of_an_aggregate()
    {
        var aggregate = Error.Aggregate([Error.Create("a"), Error.Create("b")]).WithContext("Order {Id}", "Id", 7);

        Assert.Equal(["Order 7: a", "Order 7: b"], aggregate.Children.Select(static child => child.ToString()));
    }

    [Fact]
    internal void The_redacted_text_of_an_aggregate_hides_the_sensitive_details_of_the_frames_of_its_children()
    {
        var aggregate = Error.Aggregate([Error.Create("a"), Error.Create("b")]).WithContext("Mail to {Email}", "Email", "ada@example.com", isSensitive: true);

        Assert.Equal("Mail to ***: a, Mail to ***: b", aggregate.ToRedactedString());
    }

    [Fact]
    internal void WithContext_throws_when_the_template_is_null_or_white_space_for_every_overload()
    {
        Assert.Multiple(
            () => Assert.Throws<ArgumentNullException>(() => Upload.WithContext(null!)),
            () => Assert.Throws<ArgumentNullException>(() => Upload.WithContext(null!, "Id", 1)),
            () => Assert.Throws<ArgumentNullException>(() => Upload.WithContext(null!, new ErrorDetail("Id", 1))),
            () => Assert.Throws<ArgumentException>(() => Upload.WithContext(" ", "Id", 1)),
            () => Assert.Throws<ArgumentException>(() => Upload.WithContext(" ", new ErrorDetail("Id", 1))));
    }

    [Fact]
    internal void WithContext_throws_when_the_name_is_empty_or_the_details_are_null()
    {
        Assert.Multiple(
            () => Assert.Throws<ArgumentException>(() => Upload.WithContext("Order {Id}", string.Empty, 1)),
            () => Assert.Throws<ArgumentNullException>(() => Upload.WithContext("Order {Id}", null!)));
    }

    [Fact]
    internal void A_frame_does_not_change_when_the_array_of_details_is_changed_afterwards()
    {
        var details = new[] { new ErrorDetail("Id", 1) };
        var error = Upload.WithContext("Order {Id}", details);

        details[0] = new ErrorDetail("Id", 2);

        Assert.Equal("Order 1", error.Context[0].Message);
    }

    [Fact]
    internal void Frames_are_equal_when_template_and_details_are_equal()
    {
        var first = Upload.WithContext("Order {Id}", "Id", 7).Context[0];
        var second = Error.Create("Other.").WithContext("Order {Id}", "Id", 7).Context[0];
        var other = Upload.WithContext("Order {Id}", "Id", 8).Context[0];

        Assert.Multiple(
            () => Assert.Equal(first, second),
            () => Assert.Equal(first.GetHashCode(), second.GetHashCode()),
            () => Assert.NotEqual(first, other),
            () => Assert.False(first.Equals(null)));
    }

    [Fact]
    internal void Errors_differ_when_a_frame_has_the_same_template_but_other_detail_values()
    {
        var first = Upload.WithContext("Order {Id}", "Id", 1);

        Assert.Multiple(
            () => Assert.Equal(first, Upload.WithContext("Order {Id}", "Id", 1)),
            () => Assert.NotEqual(first, Upload.WithContext("Order {Id}", "Id", 2)),
            () => Assert.NotEqual(Upload.WithContext("Order"), Upload.WithContext("Order {Id}", "Id", 1)));
    }
}
