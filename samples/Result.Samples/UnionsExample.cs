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
//  - Union support is a C# 15 feature, and .NET 11 was still a release candidate when this was written.
//  - A union gives the language an implicit conversion from each case type, so on net11.0 a T or an Error converts
//    to a Result<T> even though the library itself defines no implicit conversions. That comes from the language.
//    Use Result.Success / Result.Failure when you want to stay explicit.
//  - A Result<Error> cannot be used as a union, because both cases are the same type.
namespace Wyzely.Functional.ResultSamples;

#if NET11_0_OR_GREATER

public static class UnionsExample
{
    public static void Run(TextWriter output)
    {
        output.WriteLine("Describe a success: " + Describe(Result.Success(42)));
        output.WriteLine("Describe a failure: " + Describe(Result.Failure<int>(Error.NotFound("Order {OrderId} was not found.").WithDetail("OrderId", 7))));

        // The conversions come from the union feature of the language.
        Result<int> fromValue = 5;
        Result<int> fromError = Error.Conflict("Closed.");
        output.WriteLine("A value converts to a result: " + Describe(fromValue));
        output.WriteLine("An error converts to a result: " + Describe(fromError));
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
