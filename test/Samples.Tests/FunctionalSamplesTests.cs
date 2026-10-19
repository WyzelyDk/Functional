using Wyzely.Functional.FunctionalSamples;

namespace Wyzely.Functional.SamplesTests;

// Each example of the umbrella package prints labelled lines; these tests pin the lines that matter, so a sample cannot silently stop
// saying what its comments claim. One class, so that the example that changes the process-wide settings of both packages never overlaps
// with the others of this class.
public sealed class FunctionalSamplesTests
{
    [Fact]
    internal void AbsentToFailedExample_gives_the_error_you_chose_and_runs_a_lazy_factory_only_for_none()
    {
        var lines = Run(AbsentToFailedExample.Run);

        Assert.Multiple(
            () => Assert.Contains("Eager error, customer found: success Ada Lovelace", lines),
            () => Assert.Contains("Eager error, customer missing: failure [NotFound] No customer with that email address.", lines),
            () => Assert.Contains("Lazy error, customer found: success Ada Lovelace", lines),
            () => Assert.Contains("Lazy error, customer missing: failure [NotFound] No customer with email nobody@example.com.", lines),
            () => Assert.Contains("Lazy error, redacted message hides the email: True", lines),
            () => Assert.Contains("Error factory calls: 1", lines),
            () => Assert.Contains("Kind for an email somebody typed: NotFound", lines),
            () => Assert.Contains("Kind for an order that points at a missing customer: Unexpected", lines),
            () => Assert.Contains("A null error throws: ArgumentNullException", lines));
    }

    [Fact]
    internal void FailedToAbsentExample_drops_the_error_and_turns_a_null_success_into_none()
    {
        var lines = Run(FailedToAbsentExample.Run);

        Assert.Multiple(
            () => Assert.Contains("Result for ada@example.com: success Ada Lovelace", lines),
            () => Assert.Contains("Maybe for ada@example.com: Ada Lovelace", lines),
            () => Assert.Contains("Result for linus@example.com: failure [Forbidden] The customer is blocked.", lines),
            () => Assert.Contains("Maybe for linus@example.com: none", lines),
            () => Assert.Contains("Result for nobody@example.com: failure [NotFound] No customer with that email address.", lines),
            () => Assert.Contains("Maybe for nobody@example.com: none", lines),
            () => Assert.Contains("A blocked and a missing customer look the same as maybes: True", lines),
            () => Assert.Contains("Success with a null value: IsSuccess is True, ToMaybe is none: True", lines),
            () => Assert.Contains("Customers that passed validation: Ada Lovelace, Grace Hopper", lines),
            () => Assert.Contains("From Task<Result<Customer>>: Ada Lovelace", lines));
    }

    [Fact]
    internal void AsyncWorkflowExample_runs_one_chain_from_a_maybe_to_a_message_and_stops_at_the_first_failure()
    {
        var lines = Run(AsyncWorkflowExample.Run);

        Assert.Multiple(
            () => Assert.Contains("Customer 1 pays 250: Paid: R-1 for 250", lines),
            () => Assert.Contains("Customer 2 pays 250: Failed [Validation] Credit limit 100 is below the amount 250.", lines),
            () => Assert.Contains("Customer 3 pays 100: Failed [NotFound] No active customer with that id.", lines),
            () => Assert.Contains("Customer 4 pays 100: Failed [NotFound] No active customer with that id.", lines),
            () => Assert.Contains("Customer 5 pays 100: Failed [Unavailable] The payment gateway did not answer. Retry later.", lines),
            () => Assert.Contains("Payment gateway calls: 2", lines));
    }

    [Fact]
    internal void CollectionsExample_turns_lookups_into_results_and_combines_or_partitions_them()
    {
        var lines = Run(CollectionsExample.Run);

        Assert.Multiple(
            () => Assert.Contains("FirstOrNone, first unpaid order: 2", lines),
            () => Assert.Contains("Customer of the first unpaid order: Grace Hopper", lines),
            () => Assert.Contains("FirstOrNone, order above 1000: none", lines),
            () => Assert.Contains("Choose, customers that exist: Ada Lovelace, Grace Hopper", lines),
            () => Assert.Contains("GetValueOrNone, price of MILK: 2", lines),
            () => Assert.Contains("GetValueOrNone, price of CAKE: unknown", lines),
            () => Assert.Contains("Combine, all lines priced: 6, 6 (total 12)", lines),
            () => Assert.Contains("Combine, some lines unknown: 2 problems of kind NotFound", lines),
            () => Assert.Contains("  - Unknown SKU CAKE.", lines),
            () => Assert.Contains("  - Unknown SKU TEA.", lines),
            () => Assert.Contains("Partition: 1 priced (3), 2 rejected", lines),
            () => Assert.Contains("Maybe.Zip, both present: Grace Hopper buys MILK at 2", lines),
            () => Assert.Contains("Maybe.Zip, one missing: none", lines),
            () => Assert.Contains("Flatten, Maybe<Maybe<Customer>>: Grace Hopper", lines),
            () => Assert.Contains("Result.Zip, both failed, in argument order: No customer with that email address. | Unknown SKU CAKE.", lines));
    }

    [Fact]
    internal void ConfigurationExample_shows_that_each_package_follows_its_own_setting_and_restores_the_defaults()
    {
        var lines = Run(ConfigurationExample.Run);

        Assert.Multiple(
            () => Assert.Contains("Task<Maybe<T>>.ToResult follows Maybe : default 0, Maybe=true 1, Result=true 0", lines),
            () => Assert.Contains("Task<Result<T>>.ToMaybe follows Result: default 0, Maybe=true 0, Result=true 1", lines),
            () => Assert.Contains("Task<Maybe<T>>.Filter follows Maybe   : default 0, Maybe=true 1, Result=true 0", lines),
            () => Assert.Contains("Task<Result<T>>.Ensure follows Result : default 0, Maybe=true 0, Result=true 1", lines),
            () => Assert.Contains("Settings restored: True", lines));
    }

    [Fact]
    internal void AnalyzersExample_runs_the_forms_that_the_analyzers_accept()
    {
        var lines = Run(AnalyzersExample.Run);

        Assert.Multiple(
            () => Assert.Contains("A default Maybe is none: True", lines),
            () => Assert.Contains("TryGetValue on a some: 42", lines),
            () => Assert.Contains("TryGetValue on a none: no value", lines),
            () => Assert.Contains("IsSome guard before GetValue: Ada Lovelace", lines),
            () => Assert.Contains("Match instead of GetValue: no customer", lines),
            () => Assert.Contains("TryGetError on a failure: Unavailable", lines),
            () => Assert.Contains("IsFailure guard before GetError: Unavailable", lines),
            () => Assert.Contains("ValueOr on a failure: 0", lines));
    }

    [Fact]
    internal void CheckoutExample_places_an_order_in_one_chain_and_reports_each_kind_of_rejection()
    {
        var lines = Run(CheckoutExample.Run);
        var nobodyAndCake = "nobody@example.com: Rejected [NotFound] Checkout: No customer with that email address., " +
            "Checkout: Unknown product CAKE.";

        Assert.Multiple(
            () => Assert.Contains("ada@example.com: Paid R-ada for 8", lines),
            () => Assert.Contains("nobody@example.com: Rejected [NotFound] Checkout: No customer with that email address.", lines),
            () => Assert.Contains("linus@example.com: Rejected [Forbidden] Checkout: The customer is blocked.", lines),
            () => Assert.Contains("ada@example.com: Rejected [NotFound] Checkout: Unknown product CAKE., Checkout: Unknown product TEA.", lines),
            () => Assert.Contains("alan@example.com: Rejected [Unavailable] Checkout: The payment gateway did not answer. (retry later)", lines),
            () => Assert.Contains(nobodyAndCake, lines),
            () => Assert.Contains("Failures logged: 5", lines));
    }

    private static string[] Run(Action<TextWriter> example)
    {
        using var writer = new StringWriter();
        example(writer);

        return writer.ToString().Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries);
    }
}
