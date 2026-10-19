namespace Wyzely.Functional.SharedTests;

public sealed class OptionsHolderTests
{
    [Fact]
    internal void A_new_holder_has_the_default_settings()
    {
        var holder = CreateHolder();

        Assert.Equal("default", holder.Current.Text);
    }

    [Fact]
    internal void Configure_applies_the_change_when_the_action_returns()
    {
        var holder = CreateHolder();

        holder.Configure(static options => options.Text = "changed");

        Assert.Equal("changed", holder.Current.Text);
    }

    [Fact]
    internal void Configure_changes_nothing_when_the_action_throws()
    {
        var holder = CreateHolder();

        Assert.Throws<InvalidOperationException>(() => holder.Configure(static options =>
        {
            options.Text = "half";

            throw new InvalidOperationException("Boom.");
        }));

        Assert.Equal("default", holder.Current.Text);
    }

    [Fact]
    internal void A_reference_that_the_action_keeps_cannot_change_the_active_settings()
    {
        var holder = CreateHolder();
        Settings? kept = null;
        holder.Configure(options => kept = options);

        kept!.Text = "sneaky";

        Assert.Equal("default", holder.Current.Text);
    }

    [Fact]
    internal void The_active_settings_are_a_copy_of_what_the_action_saw()
    {
        var holder = CreateHolder();
        var before = holder.Current;

        holder.Configure(static options => options.Text = "changed");

        Assert.Multiple(
            () => Assert.Equal("default", before.Text),
            () => Assert.NotSame(before, holder.Current));
    }

    [Fact]
    internal void Reset_restores_the_defaults()
    {
        var holder = CreateHolder();
        holder.Configure(static options => options.Text = "changed");

        holder.Reset();

        Assert.Equal("default", holder.Current.Text);
    }

    [Fact]
    internal void Configure_throws_when_the_action_is_null()
    {
        Assert.Throws<ArgumentNullException>(() => CreateHolder().Configure(null!));
    }

    [Fact]
    internal async Task A_change_made_on_one_thread_is_seen_by_another()
    {
        var holder = CreateHolder();

        await Task.Run(() => holder.Configure(static options => options.Text = "from another thread"), TestContext.Current.CancellationToken);

        Assert.Equal("from another thread", await Task.Run(() => holder.Current.Text, TestContext.Current.CancellationToken));
    }

    [Fact]
    internal async Task Concurrent_changes_never_expose_a_mixture()
    {
        var holder = CreateHolder();
        var stop = false;

        var writer = Task.Run(
            () =>
            {
                for (var i = 0; i < 2_000; i++)
                {
                    holder.Configure(static options =>
                    {
                        options.Text = "a";
                        options.Number = 1;
                    });
                    holder.Configure(static options =>
                    {
                        options.Text = "b";
                        options.Number = 2;
                    });
                }

                stop = true;
            },
            TestContext.Current.CancellationToken);

        var torn = 0;

        while (!Volatile.Read(ref stop))
        {
            var seen = holder.Current;

            if ((seen.Text == "a" && seen.Number == 2) || (seen.Text == "b" && seen.Number == 1))
            {
                torn++;
            }
        }

        await writer;

        Assert.Equal(0, torn);
    }

    private static OptionsHolder<Settings> CreateHolder() => new(static () => new Settings(), static settings => settings.Clone());

    private sealed class Settings
    {
        public string Text { get; set; } = "default";

        public int Number { get; set; }

        public Settings Clone() => (Settings)MemberwiseClone();
    }
}
