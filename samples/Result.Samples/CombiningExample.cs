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
        var validSignup = Result.Combine(valid);
        output.WriteLine($"Valid signup: {validSignup}");

        // With failures: ONE failed result that holds both errors, in order.
        Result[] invalid = [ValidateName(string.Empty), ValidateEmail("not an email")];
        var combined = Result.Combine(invalid);
        var signupError = combined.GetError();
        output.WriteLine($"Invalid signup: {signupError.Message}");

        var problems = signupError.Flatten();
        output.WriteLine($"Invalid signup problems: {problems.Count}");

        // Combine over results WITH values: one result holding the list of the values, in order.
        var successes = OneTwoThree.Select(Result.Success);
        var numbers = Result.Combine(successes);
        var numberValues = numbers.ValueOr([]);
        var joinedNumbers = string.Join(", ", numberValues);
        output.WriteLine($"Typed combine values: {joinedNumbers}");

        var lookups = OneMissing.Select(SampleShop.FindOrder);
        var orders = Result.Combine(lookups);
        var ordersOutcome = orders.Match(_ => "all found", error => error.Message);
        output.WriteLine($"Typed combine with a failure: {ordersOutcome}");

        // Zip: combine a FEW independent values (2 to 5) into a tuple with named elements First, Second, Third...
        // If any of them failed you get the error, or an aggregate of all the errors in argument order.
        var existingCustomer = SampleShop.FindCustomer(1);
        var existingOrder = SampleShop.FindOrder(7);
        var pair = existingCustomer.Zip(existingOrder);
        var pairOutcome = pair.Match(both => $"{both.First.Name} has order {both.Second.Id}", error => error.Message);
        output.WriteLine("Zip: " + pairOutcome);

        var missingCustomer = SampleShop.FindCustomer(99);
        var missingOrder = SampleShop.FindOrder(98);
        var bothFail = missingCustomer.Zip(missingOrder);
        var bothFailOutcome = bothFail.Match(_ => "ok", error => error.Message);
        output.WriteLine("Zip, both fail: " + bothFailOutcome);

        // Zip with a selector skips the tuple.
        var label = existingCustomer.Zip(existingOrder, (customer, order) => $"{customer.Name}/{order.Id}");
        var labelText = label.ValueOr("?");
        output.WriteLine($"Zip with a selector: {labelText}");

        // Three values. The third is just a value here; it can be any result.
        var price = Result.Success(500);
        var three = existingCustomer.Zip(existingOrder, price);
        var threeOutcome = three.Match(all => $"{all.First.Name}, order {all.Second.Id}, {all.Third} cents", error => error.Message);
        output.WriteLine("Zip of three: " + threeOutcome);

        var twoFail = missingCustomer.Zip(missingOrder, price);
        var twoFailOutcome = twoFail.Match(_ => "ok", error => error.Message);
        output.WriteLine("Zip of three, two fail: " + twoFailOutcome);

        // Partition: keep the successes AND collect the failures, when partial success is fine
        // (for example an import where the good rows are used and the bad rows are reported).
        var mixedLookups = Mixed.Select(SampleShop.FindOrder);
        var (values, errors) = mixedLookups.Partition();
        var valueIds = values.Select(order => order.Id);
        var joinedIds = string.Join(", ", valueIds);
        var messages = errors.Select(error => error.Message);
        var joinedMessages = string.Join("; ", messages);
        output.WriteLine(
            $"Partition: {values.Count} values ({joinedIds}), " +
            $"{errors.Count} errors ({joinedMessages})");

        // LINQ query syntax: the same as Bind, so it STOPS at the first failure.
        // Use it when each step needs the values of the earlier ones, and it reads better than nested lambdas.
        var placed = from customer in SampleShop.FindCustomer(1)
                     from order in SampleShop.FindOrder(7)
                     select $"{customer.Name} placed order {order.Id}";
        var placedText = placed.ValueOr("?");
        output.WriteLine($"LINQ: {placedText}");

        var stopped = from customer in SampleShop.FindCustomer(99)
                      from order in SampleShop.FindOrder(98)
                      select customer.Name + "/" + order.Id;
        var stoppedOutcome = stopped.Match(_ => "ok", error => error.Message);
        output.WriteLine("LINQ stops at the first failure: " + stoppedOutcome);
    }

    private static Result ValidateName(string name)
    {
        var hasName = !string.IsNullOrWhiteSpace(name);

        return Result.SuccessIf(hasName, "Name is missing.");
    }

    private static Result ValidateEmail(string email)
    {
        var hasAtSign = email.Contains('@', StringComparison.Ordinal);

        return Result.SuccessIf(hasAtSign, "Email is invalid.");
    }
}
