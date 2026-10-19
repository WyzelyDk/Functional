// UNTYPED RESULT vs Result<T>.
//
// Scenario: some operations produce a value (find an order, charge a card) and some only succeed or fail
// (reserve stock, save, send an email). The first kind returns Result<T>; the second returns the untyped Result.
//
// WHY two types: forcing "no value" into Result<Unit> or Result<bool> makes signatures lie. An untyped Result is
// ONE reference wide, has no value to read (so no GetValue), and the railway has overloads for it: Bind, Map,
// Tap, TapError, MapError, Compensate, Match and Switch all exist for both.
//
//   Result<T> -> Result    implicit conversion, or ToResult(): drops the value, keeps the outcome
//   Result    -> Result<T> Map(() => value), or Bind(() => resultOfT): adds a value after the step succeeded
//   Result<T> .Bind(t => untypedResult)    gives an untyped Result
namespace Wyzely.Functional.ResultSamples;

public static class UntypedExample
{
    public static void Run(TextWriter output)
    {
        // Result<T> to Result: the lossless direction, so it is the one implicit conversion in the library.
        Result dropped = Result.Success(5);
        output.WriteLine($"Implicit conversion drops the value: {dropped}");
        output.WriteLine($"ToResult drops the value: {Result.Success(5).ToResult()}");

        // A chain that mixes both: reserve the stock (no value), THEN charge (a value).
        var receipt = SampleShop.FindOrder(7).Bind(order => SampleShop.ReserveStock(order).Bind(() => SampleShop.Charge(order)));
        output.WriteLine("Reserve then charge: " + receipt.Match(value => value.Number, error => error.Message));

        // When the untyped step fails, the next step never runs.
        var charges = 0;
        var refused = SampleShop.FindOrder(11).Bind(order => SampleShop.ReserveStock(order).Bind(() =>
        {
            charges++;

            return SampleShop.Charge(order);
        }));
        output.WriteLine("Reserve fails, charge never runs: " + refused.Match(value => value.Number, error => $"{error.Message} (charges: {charges})"));

        // Map on an untyped result adds a value once the step succeeded.
        output.WriteLine("Map from an untyped result: " + Result.Success().Map(() => 42).ValueOr(0));

        // Match on an untyped result: the success delegate takes no argument.
        var reserved = SampleShop.FindOrder(7).Bind(SampleShop.ReserveStock);
        output.WriteLine("Match on an untyped result: " + reserved.Match(() => "reserved", error => error.Message));

        // Compensate: a fallback operation after a failure. Here "no stock" is tolerated (the item is back-ordered).
        var tolerated = SampleShop.FindOrder(11).Bind(SampleShop.ReserveStock).Compensate(_ => Result.Success());
        output.WriteLine($"Compensate on an untyped result: {tolerated}");

        // Conditional factories: a failure or a success depending on a condition, with a message or an Error.
        output.WriteLine($"SuccessIf(false): {Result.SuccessIf(false, "The order is closed.")}");
        output.WriteLine($"FailureIf(false): {Result.FailureIf(false, "The order is closed.")}");

        // Combine works for untyped results too: success when all succeeded, otherwise ONE failure with every error.
        Result[] checks = [Result.SuccessIf(false, "Name is missing."), Result.Success()];
        output.WriteLine("Untyped combine: " + Result.Combine(checks).Match(() => "ok", error => error.Message));
    }
}
