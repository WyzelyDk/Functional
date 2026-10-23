using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Wyzely.Functional.MaybeTests;

// Every code sample of the Maybe README and of the pages under docs/maybe is a test here, so the documentation cannot silently rot.
// The class is in the process-wide collection because the samples read the default text of ToString() and one of them changes the settings.
[Collection(ProcessWideConfiguration.Name)]
public sealed class ReadmeExamplesTests : IDisposable
{
    public void Dispose() => MaybeConfiguration.Reset();

    // README: the example at the top, and docs/maybe/async.md: the first example.
    [Fact]
    internal async Task ReadmeTopExample_sends_mail_when_the_customer_is_active_and_has_an_email()
    {
        var message = await FindCustomerAsync(7)
            .Filter(customer => customer.IsActive)
            .Bind(FindEmailAsync)
            .Map(email => email.Trim())
            .Match(email => $"Mail sent to {email}.", () => "Nobody to mail.");

        Assert.Equal("Mail sent to ada@example.com.", message);
    }

    [Theory]
    [InlineData(8)]
    [InlineData(9)]
    [InlineData(10)]
    internal async Task ReadmeTopExample_says_nobody_to_mail_when_the_customer_is_inactive_unknown_or_has_no_email(int id)
    {
        var message = await FindCustomerAsync(id)
            .Filter(customer => customer.IsActive)
            .Bind(FindEmailAsync)
            .Map(email => email.Trim())
            .Match(email => $"Mail sent to {email}.", () => "Nobody to mail.");

        Assert.Equal("Nobody to mail.", message);
    }

    [Fact]
    internal async Task AsyncPage_chain_returns_the_email_or_unknown()
    {
        var known = await FindCustomerAsync(7)
            .Filter(customer => customer.IsActive)
            .Bind(FindEmailAsync)
            .Map(address => address.Trim())
            .ValueOr(() => "unknown");
        var inactive = await FindCustomerAsync(8)
            .Filter(customer => customer.IsActive)
            .Bind(FindEmailAsync)
            .Map(address => address.Trim())
            .ValueOr(() => "unknown");

        Assert.Multiple(
            () => Assert.Equal("ada@example.com", known),
            () => Assert.Equal("unknown", inactive));
    }

    // README: create a maybe and read it, and docs/maybe/getting-started.md: the creation snippet.
    [Fact]
    internal void ReadmeCreateExample_creates_somes_and_nones()
    {
        string? nickname = null;
        var count = 3;

        var name = Maybe.Some("Ada");
        var nobody = Maybe.None<string>();
        var nick = Maybe.From(nickname);
        var several = Maybe.SomeIf(count > 1, count);
        Maybe<string> plain = "Ada";
        var found = name.TryGetValue(out var value);

        Assert.Multiple(
            () => Assert.Equal("Some(Ada)", name.ToString()),
            () => Assert.Equal("None", nobody.ToString()),
            () => Assert.True(nick.IsNone),
            () => Assert.Equal("Some(3)", several.ToString()),
            () => Assert.Equal(name, plain),
            () => Assert.True(name.IsSome),
            () => Assert.Equal("Ada", name.ValueOr("nobody")),
            () => Assert.Equal("nobody", nobody.ValueOr("nobody")),
            () => Assert.True(found),
            () => Assert.Equal("Ada", value),
            () => Assert.Throws<ArgumentNullException>(() => Maybe.Some<string>(null!)));
    }

    [Fact]
    internal void GettingStartedCreateSnippet_converts_and_defaults_to_none_when_there_is_no_value()
    {
        string? nickname = null;
        int? score = 12;

        var fromReference = Maybe.From(nickname);
        var fromNullable = Maybe.From(score);
        Maybe<int> unassigned = default;
        Maybe<string> converted = "Ada";
        Maybe<string> convertedNull = (string?)null;

        Assert.Multiple(
            () => Assert.True(fromReference.IsNone),
            () => Assert.Equal(Maybe.Some(12), fromNullable),
            () => Assert.True(unassigned.IsNone),
            () => Assert.Equal(Maybe.Some("Ada"), converted),
            () => Assert.True(convertedNull.IsNone));
    }

    // README: chain steps.
    [Theory]
    [InlineData("ada", "ADA LOVELACE")]
    [InlineData("alan", "nobody")]
    [InlineData("zed", "nobody")]
    internal void ReadmeChainExample_describes_the_person_or_nobody(string key, string expected)
    {
        var directory = new Dictionary<string, string> { ["ada"] = "Ada Lovelace", ["alan"] = "Alan Turing" };

        string Describe(string key) => directory.GetValueOrNone(key)
            .Filter(name => name.StartsWith("Ada", StringComparison.Ordinal))
            .Map(name => name.ToUpperInvariant())
            .ValueOr("nobody");

        Assert.Equal(expected, Describe(key));
    }

    // README: ask a collection a question.
    [Fact]
    internal void ReadmeCollectionExample_finds_the_first_and_single_matches()
    {
        int[] numbers = [4, 8, 15, 16];
        IEnumerable<Maybe<int>> lookups = [Maybe.Some(1), Maybe.None<int>(), Maybe.Some(3)];

        Assert.Multiple(
            () => Assert.Equal(Maybe.Some(15), numbers.FirstOrNone(n => n > 10)),
            () => Assert.True(numbers.FirstOrNone(n => n > 100).IsNone),
            () => Assert.Equal(Maybe.Some(16), numbers.SingleOrNone(n => n > 15)),
            () => Assert.True(numbers.SingleOrNone(n => n > 10).IsNone),
            () => Assert.Equal([1, 3], lookups.Choose()));
    }

    // README: let the compiler catch the mistake, and docs/maybe/safety-and-analyzers.md: the first snippet.
    [Fact]
    internal void ReadmeSafetyExample_throws_on_GetValue_of_a_default_maybe_and_not_when_guarded()
    {
        Maybe<int> missing = default;

        var exception = Assert.Throws<InvalidOperationException>(() => missing.GetValue());
        var fine = 0;

        if (missing.IsSome)
        {
            fine = missing.GetValue();
        }

        Assert.Multiple(
            () => Assert.Equal("The maybe has no value. Check IsSome first, or use TryGetValue.", exception.Message),
            () => Assert.Equal(0, fine),
            () => Assert.True(missing.IsNone),
            () => Assert.Equal("None", missing.ToString()),
            () => Assert.Equal(7, missing.ValueOr(7)));
    }

    [Fact]
    internal void SafetyPage_TryGetValue_snippet_gives_a_non_null_value_when_it_returns_true()
    {
        var maybe = Maybe.Some("Ada");
        var length = 0;

        if (maybe.TryGetValue(out var name))
        {
            length = name.Length;
        }

        Assert.Equal(3, length);
    }

    // docs/maybe/getting-started.md
    [Fact]
    internal void GettingStartedQuickStart_welcomes_an_adult_and_turns_everyone_else_away()
    {
        static string Welcome(string text) => ParseAge(text)
            .Filter(age => age >= 18)
            .Map(age => $"Welcome, age {age}.")
            .ValueOr("Sorry, we cannot let you in.");

        Assert.Multiple(
            () => Assert.Equal("Welcome, age 42.", Welcome("42")),
            () => Assert.Equal("Sorry, we cannot let you in.", Welcome("abc")),
            () => Assert.Equal("Sorry, we cannot let you in.", Welcome("12")));
    }

    [Fact]
    internal void GettingStartedReadSnippet_reads_equality_ordering_and_text()
    {
        var some = Maybe.Some(5);
        var none = Maybe.None<int>();

        Assert.Multiple(
            () => Assert.Equal(5, some.ValueOr(0)),
            () => Assert.Equal(0, none.ValueOr(0)),
            () => Assert.Equal(5, some.AsNullable()),
            () => Assert.Null(none.AsNullable()),
            () => Assert.Empty(none.ToEnumerable()),
            () => Assert.True(some == Maybe.Some(5)),
            () => Assert.True(none == default),
            () => Assert.True(none < some),
            () => Assert.Equal("Some(5)", some.ToString()),
            () => Assert.Equal("None", none.ToString()));
    }

    // docs/maybe/the-railway.md
    [Fact]
    internal void RailwayPage_pipeline_runs_the_steps_of_a_some_and_the_alternatives_of_a_none()
    {
        var logged = new List<string>();

        var title = Maybe.Some("  Hitchhiker's Guide  ")
            .Map(text => text.Trim())
            .Filter(text => text.Length > 3)
            .Tap(text => logged.Add($"found {text}"))
            .Bind(text => text.Contains(' ') ? Maybe.Some(text) : Maybe.None<string>())
            .Or(() => Maybe.Some("untitled"))
            .Map(text => text.ToUpperInvariant())
            .Match(text => text, () => "no title");

        var empty = Maybe.None<string>()
            .TapNone(() => logged.Add("nothing to show"))
            .Or(() => Maybe.Some("untitled"))
            .ValueOr(() => "no title");

        Assert.Multiple(
            () => Assert.Equal("HITCHHIKER'S GUIDE", title),
            () => Assert.Equal("untitled", empty),
            () => Assert.Equal(["found Hitchhiker's Guide", "nothing to show"], logged));
    }

    [Fact]
    internal void RailwayPage_Map_turns_a_null_result_into_none()
    {
        var nickname = Maybe.Some(new Person(null!)).Map(person => person.Nickname);

        Assert.True(nickname.IsNone);
    }

    [Fact]
    internal void RailwayPage_Bind_with_From_turns_a_nullable_property_into_none_without_a_warning()
    {
        var nickname = Maybe.Some(new NamedPerson("Ada", null)).Bind(person => Maybe.From(person.Nickname));
        var withNickname = Maybe.Some(new NamedPerson("Ada", "Countess")).Bind(person => Maybe.From(person.Nickname));

        Assert.Multiple(
            () => Assert.True(nickname.IsNone),
            () => Assert.Equal(Maybe.Some("Countess"), withNickname));
    }

    [Fact]
    internal void RailwayPage_query_syntax_zip_and_flatten_combine_maybes()
    {
        var total = from price in Maybe.Some(40)
                    from tax in Maybe.Some(2)
                    where price > 0
                    select price + tax;

        var pair = Maybe.Some(1).Zip(Maybe.Some("one"));
        var sum = Maybe.Some(1).Zip(Maybe.Some(2), (a, b) => a + b);

        var nested = Maybe.Some(Maybe.Some(7));
        var flat = nested.Flatten();

        Assert.Multiple(
            () => Assert.Equal(Maybe.Some(42), total),
            () => Assert.Equal(Maybe.Some((1, "one")), pair),
            () => Assert.Equal(Maybe.Some(3), sum),
            () => Assert.Equal(Maybe.Some(7), flat));
    }

    // docs/maybe/async.md
    [Fact]
    internal async Task AsyncPage_async_lambda_binds_without_ambiguity()
    {
        var doubled = await Maybe.Some(21).Bind(async n =>
        {
            await Task.Yield();
            return Maybe.Some(n * 2);
        });

        Assert.Equal(Maybe.Some(42), doubled);
    }

    [Fact]
    [SuppressMessage("Style", "IDE0200", Justification = "The page shows the lambda form as a workaround.")]
    internal async Task AsyncPage_named_return_type_and_method_group_workarounds_compile()
    {
        var named = await Maybe.Some(21).Bind(async Task<Maybe<int>> (n) =>
        {
            await Task.Yield();
            return Maybe.Some(n * 2);
        });
        var group = await Maybe.Some(new Customer(7, true, null)).Bind(FindEmailAsync);
        var lambda = await Maybe.Some(new Customer(7, true, null)).Bind(customer => FindEmailAsync(customer));

        Assert.Multiple(
            () => Assert.Equal(Maybe.Some(42), named),
            () => Assert.Equal(Maybe.None<string>(), group),
            () => Assert.Equal(Maybe.None<string>(), lambda));
    }

    // docs/maybe/configuration.md
    [Fact]
    internal void ConfigurationPage_Configure_changes_the_text_of_some_and_none()
    {
        MaybeConfiguration.Configure(options =>
        {
            options.SomeFormat = "Just({0})";
            options.NoneText = "Nothing";
            options.NoValueMessage = "There is nothing to read here.";
        });

        var exception = Assert.Throws<InvalidOperationException>(() => Maybe.None<int>().GetValue());

        Assert.Multiple(
            () => Assert.Equal("Just(5)", Maybe.Some(5).ToString()),
            () => Assert.Equal("Nothing", Maybe.None<int>().ToString()),
            () => Assert.Equal("There is nothing to read here.", exception.Message));
    }

    [Fact]
    internal void ConfigurationPage_hiding_the_value_redacts_ToString_until_reset()
    {
        MaybeConfiguration.Configure(options => options.ShowValueInToString = false);
        var hidden = Maybe.Some("secret").ToString();
        var none = Maybe.None<string>().ToString();
        MaybeConfiguration.Reset();
        var restored = Maybe.Some("secret").ToString();

        Assert.Multiple(
            () => Assert.Equal("Some(***)", hidden),
            () => Assert.Equal("None", none),
            () => Assert.Equal("Some(secret)", restored));
    }

    // docs/maybe/collections.md
    [Fact]
    internal void CollectionsPage_snippet_answers_the_maybe_there_is_one_questions()
    {
        int[] numbers = [4, 8, 15, 16];
        var prices = new Dictionary<string, decimal> { ["tea"] = 3m, ["coffee"] = 4m };
        IEnumerable<Maybe<int>> lookups = [Maybe.Some(1), Maybe.None<int>(), Maybe.Some(3)];

        var lengths = new[] { "a", string.Empty, "ccc" }.Choose(text => Maybe.SomeIf(text.Length > 0, text.Length));

        Assert.Multiple(
            () => Assert.Equal(Maybe.Some(15), numbers.FirstOrNone(n => n > 10)),
            () => Assert.Equal(Maybe.Some(16), numbers.LastOrNone()),
            () => Assert.Equal(Maybe.Some(16), numbers.SingleOrNone(n => n > 15)),
            () => Assert.True(numbers.SingleOrNone(n => n > 10).IsNone),
            () => Assert.Equal(Maybe.Some(15), numbers.ElementAtOrNone(2)),
            () => Assert.True(numbers.ElementAtOrNone(10).IsNone),
            () => Assert.Equal(Maybe.Some(3m), prices.GetValueOrNone("tea")),
            () => Assert.True(prices.GetValueOrNone("water").IsNone),
            () => Assert.Equal([1, 3], lookups.Choose()),
            () => Assert.Equal([1, 3], lengths));
    }

    // README: combine maybes.
    [Fact]
    internal void ReadmeCombineExample_zips_two_and_three_maybes_with_a_selector_and_flattens()
    {
        var pair = Maybe.Some(1).Zip(Maybe.Some("one"));
        var triple = Maybe.Some(1).Zip(Maybe.Some("one"), Maybe.Some(true));
        var sum = Maybe.Some(1).Zip(Maybe.Some(2), (a, b) => a + b);
        var flat = Maybe.Some(Maybe.Some(7)).Flatten();

        Assert.Multiple(
            () => Assert.Equal((1, "one"), pair.GetValue()),
            () => Assert.Equal((1, "one", true), triple.GetValue()),
            () => Assert.Equal(3, sum.GetValue()),
            () => Assert.Equal(7, flat.GetValue()));
    }

    [Fact]
    internal void ReadmeCombineExample_zip_goes_up_to_five_maybes_and_gives_none_when_any_is_missing()
    {
        var five = Maybe.Some(1).Zip(Maybe.Some(2), Maybe.Some(3), Maybe.Some(4), Maybe.Some(5));
        var missing = Maybe.Some(1).Zip(Maybe.Some(2), Maybe.None<int>(), Maybe.Some(4), Maybe.Some(5));

        Assert.Multiple(
            () => Assert.Equal((1, 2, 3, 4, 5), five.GetValue()),
            () => Assert.True(missing.IsNone));
    }

    // README and docs/maybe/the-railway.md: a Map delegate that returns a nullable value type gives a Maybe of the value type.
    [Fact]
    internal void RailwayPage_Map_to_a_nullable_value_type_gives_a_maybe_of_the_value_type()
    {
        var known = Maybe.Some(new AgedPerson("Ada", 36)).Map(p => p.Age);
        var unknown = Maybe.Some(new AgedPerson("Bob", null)).Map(p => p.Age);
        var explicitForm = Maybe.Some(new AgedPerson("Bob", null)).Bind(p => Maybe.From(p.Age));

        Assert.Multiple(
            () => Assert.IsType<Maybe<int>>(known),
            () => Assert.Equal(36, known.GetValue()),
            () => Assert.True(unknown.IsNone),
            () => Assert.True(explicitForm.IsNone));
    }

    [Fact]
    internal void RailwayPage_Zip_with_a_selector_that_returns_a_nullable_value_type_gives_a_maybe_of_the_value_type()
    {
        var larger = Maybe.Some(5).Zip(Maybe.Some(3), (a, b) => a > b ? a : (int?)null);
        var notLarger = Maybe.Some(2).Zip(Maybe.Some(3), (a, b) => a > b ? a : (int?)null);

        Assert.Multiple(
            () => Assert.IsType<Maybe<int>>(larger),
            () => Assert.Equal(5, larger.GetValue()),
            () => Assert.True(notLarger.IsNone));
    }

    private static Maybe<int> ParseAge(string text) =>
        int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var age)
            ? Maybe.Some(age)
            : Maybe.None<int>();

    private static Task<Maybe<Customer>> FindCustomerAsync(int id) => Task.FromResult(id switch
    {
        7 => Maybe.Some(new Customer(7, true, " ada@example.com ")),
        8 => Maybe.Some(new Customer(8, false, "grace@example.com")),
        10 => Maybe.Some(new Customer(10, true, null)),
        _ => Maybe.None<Customer>(),
    });

    private static ValueTask<Maybe<string>> FindEmailAsync(Customer customer) => new(Maybe.From(customer.Email));

    private sealed record Customer(int Id, bool IsActive, string? Email);

    private sealed record Person(string Nickname);

    private sealed record NamedPerson(string Name, string? Nickname);

    private sealed record AgedPerson(string Name, int? Age);
}
