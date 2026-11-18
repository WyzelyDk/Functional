// UNTYPED RESULT vs Result<T>.
//
// Scenario: some operations produce a value (find an order, charge a card) and some only succeed or fail
// (reserve stock, save, send an email). The first kind returns Result<T>; the second returns the untyped Result.
//
// WHY two types: forcing "no value" into Result<Unit> or Result<bool> makes signatures lie. An untyped Result is
// ONE reference wide, has no value to read (so no GetValue), and the railway has overloads for it: Bind, Map,
// Tap, TapError, MapError, OrElse, Match and Switch all exist for both.
//
//   Result<T> -> Result    implicit conversion, or ToResult(): drops the value, keeps the outcome
//   Result    -> Result<T> Map(() => value), or Bind(() => resultOfT): adds a value after the step succeeded
//   Result<T> .Bind(t => untypedResult)    gives an untyped Result
namespace Wyzely.Functional.ResultSamples;

public static class UntypedExample
{
    public static void Run(TextWriter output)
    {
        // Result<T> to Result: the lossless direction: the state is kept and only the value is dropped.
        Result dropped = Result.Success(5);
        output.WriteLine($"Implicit conversion drops the value: {dropped}");
        var converted = Result.Success(5).ToResult();
        output.WriteLine($"ToResult drops the value: {converted}");

        // A chain that mixes both: reserve the stock (no value), THEN charge (a value).
        var receipt = SampleShop.FindOrder(7).Bind(order => SampleShop.ReserveStock(order)
            .Bind(() => SampleShop.Charge(order)));
        var receiptNumber = receipt.Match(value => value.Number, error => error.Message);
        output.WriteLine("Reserve then charge: " + receiptNumber);

        // When the untyped step fails, the next step never runs.
        var charges = 0;
        var refused = SampleShop.FindOrder(11).Bind(order => SampleShop.ReserveStock(order)
            .Bind(() => ChargeAndCount(order)));
        var refusal = refused.Match(value => value.Number, error => $"{error.Message} (charges: {charges})");
        output.WriteLine("Reserve fails, charge never runs: " + refusal);

        // Map on an untyped result adds a value once the step succeeded.
        var mappedValue = Result.Success().Map(() => 42).ValueOr(0);
        output.WriteLine("Map from an untyped result: " + mappedValue);

        // Match on an untyped result: the success delegate takes no argument.
        var reserved = SampleShop.FindOrder(7).Bind(SampleShop.ReserveStock);
        var reservation = reserved.Match(() => "reserved", error => error.Message);
        output.WriteLine("Match on an untyped result: " + reservation);

        // OrElse: an alternative operation after a failure. Here "no stock" is tolerated (the item is back-ordered).
        var tolerated = SampleShop.FindOrder(11).Bind(SampleShop.ReserveStock).OrElse(_ => Result.Success());
        output.WriteLine($"OrElse on an untyped result: {tolerated}");

        // Conditional factories: a failure or a success depending on a condition, with a message or an Error.
        var successIf = Result.SuccessIf(false, "The order is closed.");
        output.WriteLine($"SuccessIf(false): {successIf}");
        var failureIf = Result.FailureIf(false, "The order is closed.");
        output.WriteLine($"FailureIf(false): {failureIf}");

        // Combine works for untyped results too: success when all succeeded, otherwise ONE failure with every error.
        Result[] checks = [Result.SuccessIf(false, "Name is missing."), Result.Success()];
        var combined = Result.Combine(checks).Match(() => "ok", error => error.Message);
        output.WriteLine("Untyped combine: " + combined);

        Result<Receipt> ChargeAndCount(Order order)
        {
            charges++;

            return SampleShop.Charge(order);
        }
    }
}
