// VALIDATION AND COMBINING: Combine, Zip, Partition and LINQ query syntax.
//
// Scenario: validating a sign-up form, and loading several independent things for one screen.
//
// THE KEY DISTINCTION is what happens to the failures:
//  - Bind (and LINQ query syntax, which is Bind in disguise) STOPS at the first failure: the second step depends
//    on the first, so it cannot run.
//  - Combine, Zip and Partition look at EVERY result and report every failure, because the items are independent.
//    A user who typed a wrong name AND a wrong email wants to hear about both at once.
//
// Many failures become ONE Error (an aggregate whose Children are the individual errors), so a result is still
// one reference wide.
namespace Wyzely.Functional.ResultSamples;

public static class CombiningExample
{
    private static readonly int[] OneTwoThree = [1, 2, 3];
    private static readonly int[] OneMissing = [7, 98];
    private static readonly int[] Mixed = [7, 98, 8, 99];

    public static void Run(TextWriter output)
    {
        // Combine over results without a value: success only when every check succeeded.
        Result[] valid = [ValidateName("Anna"), ValidateEmail("anna@example.com")];
        output.WriteLine($"Valid signup: {Result.Combine(valid)}");

        // With failures: ONE failed result that holds both errors, in order.
        Result[] invalid = [ValidateName(string.Empty), ValidateEmail("not an email")];
        var combined = Result.Combine(invalid);
        output.WriteLine($"Invalid signup: {combined.GetError().Message}");
        output.WriteLine($"Invalid signup problems: {combined.GetError().Flatten().Count}");

        // Combine over results WITH values: one result holding the list of the values, in order.
        var numbers = Result.Combine(OneTwoThree.Select(Result.Success));
        output.WriteLine($"Typed combine values: {string.Join(", ", numbers.ValueOr([]))}");

        var orders = Result.Combine(OneMissing.Select(SampleShop.FindOrder));
        output.WriteLine($"Typed combine with a failure: {orders.Match(_ => "all found", error => error.Message)}");

        // Zip: combine a FEW independent values (2 to 5) into a tuple with named elements First, Second, Third...
        // If any of them failed you get the error, or an aggregate of all the errors in argument order.
        var pair = SampleShop.FindCustomer(1).Zip(SampleShop.FindOrder(7));
        output.WriteLine("Zip: " + pair.Match(both => $"{both.First.Name} has order {both.Second.Id}", error => error.Message));

        var bothFail = SampleShop.FindCustomer(99).Zip(SampleShop.FindOrder(98));
        output.WriteLine("Zip, both fail: " + bothFail.Match(_ => "ok", error => error.Message));

        // Zip with a selector skips the tuple.
        var label = SampleShop.FindCustomer(1).Zip(SampleShop.FindOrder(7), (customer, order) => $"{customer.Name}/{order.Id}");
        output.WriteLine($"Zip with a selector: {label.ValueOr("?")}");

        // Three values. The third is just a value here; it can be any result.
        var three = SampleShop.FindCustomer(1).Zip(SampleShop.FindOrder(7), Result.Success(500));
        output.WriteLine("Zip of three: " + three.Match(all => $"{all.First.Name}, order {all.Second.Id}, {all.Third} cents", error => error.Message));

        var twoFail = SampleShop.FindCustomer(99).Zip(SampleShop.FindOrder(98), Result.Success(500));
        output.WriteLine("Zip of three, two fail: " + twoFail.Match(_ => "ok", error => error.Message));

        // Partition: keep the successes AND collect the failures, when partial success is fine
        // (for example an import where the good rows are used and the bad rows are reported).
        var (values, errors) = Mixed.Select(SampleShop.FindOrder).Partition();
        output.WriteLine(
            $"Partition: {values.Count} values ({string.Join(", ", values.Select(order => order.Id))}), " +
            $"{errors.Count} errors ({string.Join("; ", errors.Select(error => error.Message))})");

        // LINQ query syntax: the same as Bind, so it STOPS at the first failure. Use it when each step needs
        // the values of the earlier ones, and it reads better than nested lambdas.
        var placed = from customer in SampleShop.FindCustomer(1)
                     from order in SampleShop.FindOrder(7)
                     select $"{customer.Name} placed order {order.Id}";
        output.WriteLine($"LINQ: {placed.ValueOr("?")}");

        var stopped = from customer in SampleShop.FindCustomer(99)
                      from order in SampleShop.FindOrder(98)
                      select customer.Name + "/" + order.Id;
        output.WriteLine("LINQ stops at the first failure: " + stopped.Match(_ => "ok", error => error.Message));
    }

    private static Result ValidateName(string name) => Result.SuccessIf(!string.IsNullOrWhiteSpace(name), "Name is missing.");

    private static Result ValidateEmail(string email) => Result.SuccessIf(email.Contains('@', StringComparison.Ordinal), "Email is invalid.");
}
