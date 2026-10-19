namespace Wyzely.Functional.MaybeTests;

[Collection(ProcessWideConfiguration.Name)]
public sealed class MaybeConfigurationTests : IDisposable
{
    public void Dispose() => MaybeConfiguration.Reset();

    [Fact]
    internal void The_defaults_are_the_documented_ones()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => Maybe.None<int>().GetValue());

        Assert.Multiple(
            () => Assert.Equal("Some(1)", Maybe.Some(1).ToString()),
            () => Assert.Equal("None", Maybe.None<int>().ToString()),
            () => Assert.Equal("The maybe has no value. Check IsSome first, or use TryGetValue.", exception.Message));
    }

    [Fact]
    internal void SomeFormat_changes_how_a_value_is_shown()
    {
        MaybeConfiguration.Configure(options => options.SomeFormat = "Just {0}");

        Assert.Equal("Just 5", Maybe.Some(5).ToString());
    }

    [Fact]
    internal void NoneText_changes_how_the_absence_of_a_value_is_shown()
    {
        MaybeConfiguration.Configure(options => options.NoneText = "Nothing");

        Assert.Equal("Nothing", Maybe.None<int>().ToString());
    }

    [Fact]
    internal void NoValueMessage_changes_the_message_of_GetValue_on_none()
    {
        MaybeConfiguration.Configure(options => options.NoValueMessage = "Nothing there.");

        var exception = Assert.Throws<InvalidOperationException>(() => Maybe.None<int>().GetValue());

        Assert.Equal("Nothing there.", exception.Message);
    }

    [Fact]
    internal void Hiding_the_value_replaces_it_with_the_redacted_text_in_ToString()
    {
        MaybeConfiguration.Configure(options =>
        {
            options.ShowValueInToString = false;
            options.RedactedValue = "<hidden>";
        });

        Assert.Equal("Some(<hidden>)", Maybe.Some("secret").ToString());
    }

    [Fact]
    internal void Hiding_the_value_does_not_change_the_text_of_none()
    {
        MaybeConfiguration.Configure(options => options.ShowValueInToString = false);

        Assert.Equal("None", Maybe.None<string>().ToString());
    }

    [Fact]
    internal void ContinueOnCapturedContext_is_off_by_default()
    {
        Assert.False(MaybeConfiguration.ContinueOnCapturedContext);
    }

    [Fact]
    internal void ContinueOnCapturedContext_can_be_turned_on()
    {
        MaybeConfiguration.Configure(options => options.ContinueOnCapturedContext = true);

        Assert.True(MaybeConfiguration.ContinueOnCapturedContext);
    }

    [Fact]
    internal void SomeFormat_must_contain_the_placeholder()
    {
        Assert.Throws<ArgumentException>(() => MaybeConfiguration.Configure(options => options.SomeFormat = "Some"));
    }

    [Fact]
    internal void SomeFormat_must_be_a_valid_format_string()
    {
        var exception = Assert.Throws<ArgumentException>(() => MaybeConfiguration.Configure(options => options.SomeFormat = "Some {0} {1}"));

        Assert.Multiple(
            () => Assert.StartsWith("The format is not a valid format string", exception.Message, StringComparison.Ordinal),
            () => Assert.IsType<FormatException>(exception.InnerException));
    }

    [Fact]
    internal void Settings_reject_null_and_blank_text()
    {
        Assert.Multiple(
            () => Assert.Throws<ArgumentNullException>(() => MaybeConfiguration.Configure(options => options.SomeFormat = null!)),
            () => Assert.Throws<ArgumentNullException>(() => MaybeConfiguration.Configure(options => options.NoneText = null!)),
            () => Assert.Throws<ArgumentNullException>(() => MaybeConfiguration.Configure(options => options.RedactedValue = null!)),
            () => Assert.Throws<ArgumentException>(() => MaybeConfiguration.Configure(options => options.NoValueMessage = " ")));
    }

    [Fact]
    internal void Configure_throws_when_the_action_is_null()
    {
        Assert.Throws<ArgumentNullException>(() => MaybeConfiguration.Configure(null!));
    }

    [Fact]
    internal void A_failing_configure_action_changes_nothing()
    {
        Assert.Throws<ArgumentException>(() => MaybeConfiguration.Configure(options =>
        {
            options.NoneText = "Changed";
            options.SomeFormat = "no placeholder";
        }));

        Assert.Equal("None", Maybe.None<int>().ToString());
    }

    [Fact]
    internal void Reset_restores_the_defaults()
    {
        MaybeConfiguration.Configure(options => options.NoneText = "Nothing");

        MaybeConfiguration.Reset();

        Assert.Equal("None", Maybe.None<int>().ToString());
    }

    [Fact]
    internal void The_active_settings_cannot_be_changed_through_a_kept_reference()
    {
        MaybeOptions? kept = null;
        MaybeConfiguration.Configure(options => kept = options);

        kept!.NoneText = "Sneaky";

        Assert.Equal("None", Maybe.None<int>().ToString());
    }

    [Fact]
    internal async Task The_async_methods_do_not_continue_on_the_captured_context_by_default()
    {
        var posts = await CountPostsOfMapOnPendingTaskAsync();

        Assert.Equal(0, posts);
    }

    [Fact]
    internal async Task ContinueOnCapturedContext_makes_the_async_methods_continue_on_the_captured_context()
    {
        MaybeConfiguration.Configure(options => options.ContinueOnCapturedContext = true);

        var posts = await CountPostsOfMapOnPendingTaskAsync();

        Assert.Equal(1, posts);
    }

    private static async Task<int> CountPostsOfMapOnPendingTaskAsync()
    {
        var previous = SynchronizationContext.Current;
        var context = new CountingSynchronizationContext();
        SynchronizationContext.SetSynchronizationContext(context);

        try
        {
            var source = new TaskCompletionSource<Maybe<int>>(TaskCreationOptions.RunContinuationsAsynchronously);
            var mapped = source.Task.Map(value => value + 1);
            source.SetResult(Maybe.Some(1));
            await mapped.ConfigureAwait(false);

            return context.Posts;
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);
        }
    }
}
