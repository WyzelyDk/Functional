namespace Wyzely.Functional.ResultTests;

// Throws from a delegate in generated tests without the compiler seeing that the code after it is unreachable.
internal static class Boom
{
    // Throws an exception, in place of a statement.
    internal static void Throw() => throw new InvalidOperationException("Boom.");

    // Throws an exception, in place of a value.
    internal static T Throw<T>() => throw new InvalidOperationException("Boom.");
}
