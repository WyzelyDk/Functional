// C# 15 UNIONS (the net11.0 build only).
//
// Scenario: you want the COMPILER to check that every outcome of a result is handled, in a plain `switch`.
//
// HOW: on a net11.0 build, Result<T> opts in as a union of its two cases, T and Error (a [Union] type with a
// nested IUnionMembers). A switch expression that handles both cases needs no discard arm, and leaving one out is a
// compiler warning. This file only has that code when the project is built with an SDK 11 (the net11.0 target);
// on other builds the example says so and does nothing else.
//
// Things to know:
//  - Union support is a C# 15 feature, so it needs the .NET 11 SDK.
//  - A union gives the language an implicit conversion from each case type, and Result<T> has the same conversions on
//    every target: the library defines them itself. Use Result.Success / Result.Failure when you want the intent visible.
//  - A Result<Error> cannot be used as a union, because both cases are the same type.
namespace Wyzely.Functional.ResultSamples;

#if NET11_0_OR_GREATER

public static class UnionsExample
{
    public static void Run(TextWriter output)
    {
        var success = Result.Success(42);
        var notFound = Error.NotFound("Order {OrderId} was not found.").WithDetail("OrderId", 7);
        var failure = Result.Failure<int>(notFound);
        var successText = Describe(success);
        var failureText = Describe(failure);
        output.WriteLine("Describe a success: " + successText);
        output.WriteLine("Describe a failure: " + failureText);

        // The conversions are the same on every target; on net11.0 they match those of a union.
        Result<int> fromValue = 5;
        Result<int> fromError = Error.Conflict("Closed.");
        var fromValueText = Describe(fromValue);
        var fromErrorText = Describe(fromError);
        output.WriteLine("A value converts to a result: " + fromValueText);
        output.WriteLine("An error converts to a result: " + fromErrorText);
    }

    // No discard arm: the compiler knows that a Result<int> is either an int or an Error.
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Style",
        "IDE0072",
        Justification = "The IDE analyzer does not know yet that a union switch over its two case types is complete; the compiler does.")]
    private static string Describe(Result<int> result) => result switch
    {
        int value => $"ok: {value}",
        Error error => $"failed: {error.Message}",
    };
}

#else

public static class UnionsExample
{
    public static void Run(TextWriter output) =>
        output.WriteLine("C# 15 unions need the net11.0 target; this build does not have it, so the example is skipped.");
}

#endif
