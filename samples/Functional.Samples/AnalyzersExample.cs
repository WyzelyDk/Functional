namespace Wyzely.Functional.FunctionalSamples;

// SCENARIO: a team installs Wyzely.Functional and writes the mistakes that everybody writes the first week.
//
// WHY: the analyzers of both packages arrive with the umbrella package as dependencies, so there is nothing else to install. Each rule
// is a warning by default (and Release builds of this repository turn warnings into errors), which is why the mistakes below are
// COMMENTS: code that triggers a rule would not compile here. The compiled code under each comment is the form the analyzer accepts.
//
// This sample project references the library's SOURCE projects, and a project reference does not run the analyzers (only an installed
// package does). So here the mistakes would not be reported at all; they are commented out to show what a consumer's build reports.
//
// The rules (each has a page under docs/result/analyzers or docs/maybe/analyzers):
//   WYZ0101  a Result is not used                 (a discarded result hides a failure; has a code fix)
//   WYZ0102  check the state of a Result before GetError() or GetValue()
//   WYZ0201  check that a Maybe has a value before GetValue()
// To make one stricter (or quieter) use the usual .editorconfig line, for example:  dotnet_diagnostic.WYZ0102.severity = error
public static class AnalyzersExample
{
    public static void Run(TextWriter output)
    {
        var directory = new CustomerDirectory();

        // WYZ0201: GetValue() on a Maybe that nothing has checked. It throws InvalidOperationException for none.
        //
        //     var customer = directory.FindByEmail("nobody@example.com").GetValue();    // WYZ0201
        //
        // A default Maybe is simply none, so there is no rule for it, and it is safe to read:
        Maybe<int> unset = default;
        output.WriteLine($"A default Maybe is none: {unset.IsNone}");

        // The forms the analyzer accepts: TryGetValue, a guard on the same variable, Match, ValueOr.
        var answer = Maybe.Some(42);

        if (answer.TryGetValue(out var number))
        {
            output.WriteLine($"TryGetValue on a some: {number}");
        }

        if (!unset.TryGetValue(out _))
        {
            output.WriteLine("TryGetValue on a none: no value");
        }

        var ada = directory.FindByEmail("ada@example.com");

        if (ada.IsSome)
        {
            output.WriteLine($"IsSome guard before GetValue: {ada.GetValue().Name}");
        }

        output.WriteLine($"Match instead of GetValue: {directory.FindByEmail("nobody@example.com").Match(customer => customer.Name, () => "no customer")}");

        // WYZ0101: ignoring what a call returns (also for Task<Result> and ValueTask<Result>). The code fix assigns it or discards it.
        //
        //     Charge(customer);                                                          // WYZ0101
        //
        // WYZ0102: GetError() or GetValue() on a Result that nothing has checked. GetValue() throws ResultFailedException for a
        // failure, GetError() throws InvalidOperationException for a success.
        //
        //     var error = Charge(customer).GetError();                                   // WYZ0102
        var failure = Result.Failure<Receipt>(Error.Unavailable("The payment gateway did not answer."));

        if (failure.TryGetError(out var error))
        {
            output.WriteLine($"TryGetError on a failure: {error.Kind}");
        }

        if (failure.IsFailure)
        {
            output.WriteLine($"IsFailure guard before GetError: {failure.GetError().Kind}");
        }

        output.WriteLine($"ValueOr on a failure: {failure.Map(receipt => receipt.Amount).ValueOr(0)}");
    }
}
