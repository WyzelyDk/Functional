// BASICS: creating a result and reading it.
//
// Scenario: look up an order. "The order does not exist" is an ordinary outcome of that operation, so it is
// returned as a value (a failed Result) instead of being thrown. The caller sees it in the signature and has to
// decide what to do with it.
//
// WHY it is written this way:
//  - Results are created with Result.Success(...) and Result.Failure(...). Nothing converts to a result
//    implicitly, so `return false;` in a method that returns Result<bool> can never be mistaken for a failure.
//  - GetValue() and GetError() are METHODS, not properties. A property that throws for the wrong state looks
//    harmless in a debugger and in a watch window; a method call signals that something can go wrong, and an
//    analyzer (WYZ0102) warns when the call is not guarded by a check.
//  - Prefer the readers that cannot throw for the wrong state: TryGetValue, TryGetError, ValueOr, Match.
namespace Wyzely.Functional.ResultSamples;

public static class BasicsExample
{
    public static void Run(TextWriter output)
    {
        // A success and a failure of the same type. Both are plain values: no exception has been thrown.
        var ok = Result.Success(42);
        var failed = Result.Failure<int>(Error.NotFound("Order {OrderId} was not found.").WithDetail("OrderId", 7));

        // The state.
        output.WriteLine($"ok.IsSuccess: {ok.IsSuccess}");
        output.WriteLine($"failed.IsFailure: {failed.IsFailure}");

        // TryGetValue / TryGetError follow the familiar TryXxx pattern. The out value is only usable when the
        // method returns true, and the compiler knows it ([MaybeNullWhen(false)] / [NotNullWhen(true)]).
        if (ok.TryGetValue(out var value))
        {
            output.WriteLine($"TryGetValue on the success: {value}");
        }

        if (failed.TryGetError(out var error))
        {
            output.WriteLine($"TryGetError on the failure: {error.Message}");
        }

        // Match leaves the railway: one value out, whichever way the result went. Both branches are required,
        // so a failure cannot be forgotten.
        output.WriteLine("Match on the success: " + ok.Match(number => $"value {number}", problem => $"error {problem.Message}"));
        output.WriteLine("Match on the failure: " + failed.Match(number => $"value {number}", problem => $"error {problem.Message}"));

        // ValueOr is the shortest way to say "or else this".
        output.WriteLine($"ValueOr on the failure: {failed.ValueOr(-1)}");

        // GetValue() on a failure throws, and the exception carries the error. The text is redacted: sensitive
        // details never end up in an exception message.
        try
        {
            _ = failed.GetValue();
        }
        catch (ResultFailedException exception)
        {
            output.WriteLine($"GetValue on the failure throws ResultFailedException: {exception.Message}");
        }

        // GetError() on a success has no error to give, so it throws too.
        try
        {
            _ = ok.GetError();
        }
        catch (InvalidOperationException)
        {
            output.WriteLine("GetError on the success throws InvalidOperationException.");
        }

        // Deconstruction is the one reader that never throws and is not checked by the analyzer: both parts are
        // always there, and one of them is empty. Use it when you really want both.
        var (okValue, okError) = ok;
        output.WriteLine($"Deconstruct of the success: value={okValue}, error={(okError is null ? "none" : okError.Message)}");

        // ToString is safe in every state, so logging and debuggers never crash. It prints the REDACTED text.
        output.WriteLine($"ToString of the failure: {failed}");
        output.WriteLine($"ToString of an untyped success: {Result.Success()}");

        // A struct always has a default value, and for a result it is a success: default(Result) is Result.Success() and
        // default(Result<int>) is Result.Success(0). A field or an array slot that nobody filled in therefore holds a
        // success, so collect results with Result.Combine or SelectInParallel instead of in an array that you fill yourself.
        var unfilled = default(Result<int>);

        output.WriteLine($"A default result: ToString is {unfilled} and IsSuccess is {unfilled.IsSuccess}.");
    }
}
