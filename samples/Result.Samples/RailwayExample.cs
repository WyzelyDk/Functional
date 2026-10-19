// THE RAILWAY: Map, Bind, Tap, TapError, MapError, Ensure, Check, Recover, Compensate, Match, Switch, Flatten.
//
// Scenario: checking out an order. Find it, make sure it is open, reserve the stock, charge the customer,
// and add where it happened to a failure.
//
// THE IDEA: think of two tracks. A result starts on the success track and each step runs only while it stays
// there. The first step that fails switches to the failure track, and every later "success" step is skipped
// without a single `if`. Some steps work on the failure track (TapError, MapError, Recover, Compensate), and
// Match / Switch leave the railway at the end.
//
//   Map         success  transforms the value
//   Bind        success  chains an operation that itself returns a Result
//   Tap         success  a side effect (logging); the result passes through unchanged
//   Ensure      success  fails when a condition on the value is false
//   Check       success  validates with an operation that returns a Result; the original value is kept
//   TapError    failure  a side effect with the error
//   MapError    failure  replaces the error, for example to add context
//   Recover     failure  computes a replacement VALUE: the failure becomes a success
//   Compensate  failure  tries a fallback OPERATION that returns a Result
//   Match       both     leaves the railway: one value out
//   Switch      both     leaves the railway for side effects only
namespace Wyzely.Functional.ResultSamples;

public static class RailwayExample
{
    public static void Run(TextWriter output)
    {
        // The whole checkout reads as one chain. Try the four interesting orders.
        foreach (var orderId in new[] { 7, 8, 99, 11 })
        {
            var outcome = Checkout(orderId, output).Match(
                receipt => $"receipt {receipt.Number} for {receipt.AmountInCents} cents",
                error => error.ToString());

            output.WriteLine($"Checkout {orderId}: {outcome}");
        }

        // Map: transform the value. A failure passes through untouched, and the lambda is not called.
        output.WriteLine("Map: " + SampleShop.FindCustomer(1).Map(customer => customer.Name).ValueOr("?"));

        // TapError: look at a failure without changing it, typically to log it. It sits BEFORE any MapError here,
        // so it sees the original error.
        _ = SampleShop.FindOrder(99).TapError(error => output.WriteLine($"TapError: logged failure {error.Message}"));

        // MapError: replace the error. Here a lookup failure becomes a validation failure for the user.
        var mapped = SampleShop.FindCustomer(99)
            .MapError(error => error.Kind == ErrorKind.NotFound ? Error.Validation("Unknown customer.") : error);
        output.WriteLine("MapError: " + mapped.Match(_ => "found", error => $"{error.Message} ({error.Kind})"));

        // Recover: a failure becomes a success with a replacement VALUE. Use it for defaults.
        var guest = SampleShop.FindCustomer(99).Recover(_ => new Customer(0, "Guest", string.Empty, IsBlocked: false));
        output.WriteLine("Recover: " + guest.Match(customer => customer.Name, error => error.Message));

        // Compensate: a failure triggers a fallback OPERATION, which can itself fail. Only a transient failure is
        // compensated here (the gateway is down); a blocked customer stays a failure.
        foreach (var orderId in new[] { 12, 9 })
        {
            var paid = SampleShop.FindOrder(orderId).Bind(order => SampleShop.Charge(order).Compensate(
                error => error.IsTransient ? SampleShop.Invoice(order) : Result.Failure<Receipt>(error)));

            output.WriteLine($"Compensate order {orderId}: " + paid.Match(receipt => receipt.Number, error => error.Message));
        }

        // Ensure with a message instead of an Error: a quick validation of the value.
        var empty = Result.Success(new Order(20, 1, [], IsOpen: true)).Ensure(order => order.Lines.Count > 0, "The order has no lines.");
        output.WriteLine("Ensure with a message: " + empty.Match(_ => "ok", error => error.Message));

        // Check: validate with an operation that returns a Result, and KEEP the original value. Compare with
        // Bind(ReserveStock), which would replace the value with what ReserveStock returns (nothing).
        var checkedOrder = SampleShop.FindOrder(7).Check(SampleShop.ReserveStock).Map(order => order.Id);
        output.WriteLine($"Check keeps the order: {checkedOrder.ValueOr(-1)}");

        // Bind into an untyped Result: the chain simply continues with a result that has no value.
        output.WriteLine($"Bind to an untyped result: {SampleShop.FindOrder(7).Bind(SampleShop.ReserveStock)}");

        // Switch: leave the railway for side effects only.
        SampleShop.FindOrder(99).Switch(
            order => output.WriteLine($"Switch: success {order.Id}"),
            error => output.WriteLine($"Switch: failure {error.Message}"));

        // Flatten: a Result<Result<T>> becomes a Result<T>. It appears when a Map delegate returns a Result
        // (use Bind to avoid that), or when you meet a nested result from somewhere else.
        var nested = Result.Success(Result.Success(5));
        output.WriteLine($"Flatten: {nested.Flatten()}");
    }

    private static Result<Receipt> Checkout(int orderId, TextWriter output) =>
        SampleShop.FindOrder(orderId)
            .Ensure(order => order.IsOpen, Error.Conflict("The order is closed."))
            .Check(SampleShop.ReserveStock)
            .Bind(SampleShop.Charge)
            .Tap(receipt => output.WriteLine($"Tap: logged receipt {receipt.Number}"))
            .MapError(error => error.WithContext("Checkout"));
}
