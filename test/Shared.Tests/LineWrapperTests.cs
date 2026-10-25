using Wyzely.Functional.CodeGeneration;

namespace Wyzely.Functional.SharedTests;

public sealed class LineWrapperTests
{
    [Fact]
    internal void Wrap_returns_the_text_unchanged_when_it_fits()
    {
        var lines = LineWrapper.Wrap("var x = Call(a, b);", indent: 4, maxWidth: 60, docWidth: 50);

        Assert.Equal(["var x = Call(a, b);"], lines);
    }

    [Fact]
    internal void Wrap_counts_the_indentation_of_the_writer_as_part_of_the_width()
    {
        var text = "var x = Call(first, second);";

        Assert.Multiple(
            () => Assert.Single(LineWrapper.Wrap(text, indent: 0, maxWidth: 40, docWidth: 40)),
            () => Assert.Equal(["var x = Call(", "    first,", "    second);"], LineWrapper.Wrap(text, indent: 20, maxWidth: 40, docWidth: 40)));
    }

    [Fact]
    internal void Wrap_puts_each_argument_of_the_last_call_on_its_own_line()
    {
        var lines = LineWrapper.Wrap(
            "var outcome = await receiver.Match(first => { return 1; }, second => { return 2; });",
            indent: 0,
            maxWidth: 60,
            docWidth: 50);

        Assert.Equal(["var outcome = await receiver.Match(", "    first => { return 1; },", "    second => { return 2; });"], lines);
    }

    [Fact]
    internal void Wrap_does_not_split_at_commas_inside_strings_or_nested_calls()
    {
        var lines = LineWrapper.Wrap("Assert.Equal(Format(\"a, b\", Pair(1, 2)), outcome.Describe(\"x, y\", mode));", indent: 0, maxWidth: 60, docWidth: 50);

        Assert.Equal(["Assert.Equal(", "    Format(\"a, b\", Pair(1, 2)),", "    outcome.Describe(\"x, y\", mode));"], lines);
    }

    [Fact]
    internal void Wrap_breaks_the_parameter_list_and_not_a_tuple_in_the_return_type()
    {
        var lines = LineWrapper.Wrap(
            "public static Maybe<(T1 First, T2 Second)> Zip<T1, T2>(this Maybe<T1> first, Maybe<T2> second)",
            indent: 4,
            maxWidth: 80,
            docWidth: 70);

        Assert.Equal(["public static Maybe<(T1 First, T2 Second)> Zip<T1, T2>(", "    this Maybe<T1> first,", "    Maybe<T2> second)"], lines);
    }

    [Fact]
    internal void Wrap_breaks_a_long_condition_before_each_logical_operator()
    {
        var lines = LineWrapper.Wrap(
            "if (first.Unpack(out var a) && second.Unpack(out var b) && third.Unpack(out var c))",
            indent: 0,
            maxWidth: 60,
            docWidth: 50);

        Assert.Equal(["if (first.Unpack(out var a)", "    && second.Unpack(out var b)", "    && third.Unpack(out var c))"], lines);
    }

    [Fact]
    internal void Wrap_leaves_a_declaration_without_arguments_alone_because_its_name_cannot_be_broken()
    {
        var text = "internal async Task A_name_that_is_far_too_long_to_fit_on_the_line_with_its_type()";

        Assert.Equal([text], LineWrapper.Wrap(text, indent: 4, maxWidth: 70, docWidth: 60));
    }

    [Fact]
    internal void Wrap_wraps_an_argument_again_when_it_is_still_too_wide()
    {
        var lines = LineWrapper.Wrap(
            "Outer(Inner(first_argument_is_long, second_argument_is_long, third_argument_is_long));",
            indent: 0,
            maxWidth: 50,
            docWidth: 50);

        Assert.Equal(
            [
                "Outer(",
                "    Inner(",
                "        first_argument_is_long,",
                "        second_argument_is_long,",
                "        third_argument_is_long));",
            ],
            lines);
    }

    [Fact]
    internal void Wrap_opens_a_lambda_with_a_block_body_and_wraps_its_statement()
    {
        var lines = LineWrapper.Wrap("() => { _ = Call(first_argument, second_argument); });", indent: 0, maxWidth: 40, docWidth: 40);

        Assert.Equal(["() =>", "{", "    _ = Call(", "        first_argument,", "        second_argument);", "});"], lines);
    }

    [Fact]
    internal void Wrap_turns_a_long_documentation_element_into_an_opening_line_wrapped_text_and_a_closing_line()
    {
        var lines = LineWrapper.Wrap(
            "/// <summary>Chains an operation that may produce no value; it runs only when there is one.</summary>",
            indent: 4,
            maxWidth: 60,
            docWidth: 40);

        Assert.Equal(
            [
                "/// <summary>",
                "/// Chains an operation that may",
                "/// produce no value; it runs only",
                "/// when there is one.",
                "/// </summary>",
            ],
            lines);
    }

    [Fact]
    internal void Wrap_keeps_a_documentation_tag_in_one_piece()
    {
        var lines = LineWrapper.Wrap(
            "/// <returns>The value is returned in a <see cref=\"Task{TResult}\"/> when the receiver has one, otherwise nothing.</returns>",
            indent: 4,
            maxWidth: 60,
            docWidth: 50);

        Assert.Contains(lines, line => line.Contains("<see cref=\"Task{TResult}\"/>", StringComparison.Ordinal));
    }

    [Fact]
    internal void Wrap_wraps_a_long_comment_at_word_boundaries()
    {
        var lines = LineWrapper.Wrap("// one two three four five six seven eight nine ten eleven twelve", indent: 0, maxWidth: 30, docWidth: 30);

        Assert.Equal(["// one two three four five six", "// seven eight nine ten eleven", "// twelve"], lines);
    }

    [Fact]
    internal void Wrap_leaves_a_line_alone_when_there_is_no_place_to_break_it()
    {
        var text = "var thisIsOneVeryLongIdentifierWithNothingToBreakAt = anotherVeryLongIdentifierWithNothingToBreakAt;";

        Assert.Equal([text], LineWrapper.Wrap(text, indent: 0, maxWidth: 40, docWidth: 40));
    }

    [Fact]
    internal void Wrap_keeps_the_leading_white_space_of_the_text_on_every_line()
    {
        var lines = LineWrapper.Wrap("    var x = Call(first, second);", indent: 0, maxWidth: 30, docWidth: 30);

        Assert.Equal(["    var x = Call(", "        first,", "        second);"], lines);
    }

    [Fact]
    internal void Wrap_breaks_a_long_condition_without_a_logical_operator_like_a_call_instead_of_before_an_operator()
    {
        var lines = LineWrapper.Wrap("if (thisConditionIsOneVeryLongNameWithNoOperatorAndNoCallToBreakAt)", indent: 0, maxWidth: 30, docWidth: 30);

        Assert.Equal(["if (", "    thisConditionIsOneVeryLongNameWithNoOperatorAndNoCallToBreakAt)"], lines);
    }

    [Fact]
    internal void Wrap_does_not_split_at_a_comma_or_a_quote_inside_a_character_literal()
    {
        var lines = LineWrapper.Wrap("var x = Call(',', second, '\\'', third);", indent: 0, maxWidth: 30, docWidth: 30);

        Assert.Equal(["var x = Call(", "    ',',", "    second,", "    '\\'',", "    third);"], lines);
    }

    [Fact]
    internal void Wrap_takes_the_brackets_after_an_underscore_and_before_a_tuple_for_type_arguments()
    {
        var lines = LineWrapper.Wrap("var x = Call_<(int, int)>(first, second, third);", indent: 0, maxWidth: 30, docWidth: 30);

        Assert.Equal(["var x = Call_<(int, int)>(", "    first,", "    second,", "    third);"], lines);
    }

    [Fact]
    internal void Wrap_does_not_take_a_less_than_that_follows_a_name_and_precedes_a_number_for_type_arguments()
    {
        var lines = LineWrapper.Wrap("var x = Call(first<1, second, third);", indent: 0, maxWidth: 30, docWidth: 30);

        Assert.Equal(["var x = Call(", "    first<1,", "    second,", "    third);"], lines);
    }

    [Fact]
    internal void Wrap_does_not_take_a_comparison_for_the_brackets_of_type_arguments()
    {
        var lines = LineWrapper.Wrap("var x = Call(first < second, third > fourth, fifth);", indent: 0, maxWidth: 30, docWidth: 30);

        Assert.Equal(["var x = Call(", "    first < second,", "    third > fourth,", "    fifth);"], lines);
    }
}
