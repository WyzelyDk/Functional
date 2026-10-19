namespace Wyzely.Functional.Internal;

/// <summary>
/// Combines hash codes. It uses <c>System.HashCode</c> where the runtime has it and a simple fold on netstandard2.0, where
/// the type is not available without another package.
/// </summary>
internal static class HashCodeHelper
{
    /// <summary>Combines two hash codes.</summary>
    /// <param name="first">The first hash code.</param>
    /// <param name="second">The second hash code.</param>
    /// <returns>A hash code that depends on both values and on their order.</returns>
    internal static int Combine(int first, int second) =>
#if NETSTANDARD2_0
        unchecked((first * 397) ^ second);
#else
        HashCode.Combine(first, second);
#endif

    /// <summary>Combines three hash codes.</summary>
    /// <param name="first">The first hash code.</param>
    /// <param name="second">The second hash code.</param>
    /// <param name="third">The third hash code.</param>
    /// <returns>A hash code that depends on all values and on their order.</returns>
    internal static int Combine(int first, int second, int third) =>
#if NETSTANDARD2_0
        Combine(Combine(first, second), third);
#else
        HashCode.Combine(first, second, third);
#endif

    /// <summary>Combines four hash codes.</summary>
    /// <param name="first">The first hash code.</param>
    /// <param name="second">The second hash code.</param>
    /// <param name="third">The third hash code.</param>
    /// <param name="fourth">The fourth hash code.</param>
    /// <returns>A hash code that depends on all values and on their order.</returns>
    internal static int Combine(int first, int second, int third, int fourth) =>
#if NETSTANDARD2_0
        Combine(Combine(first, second, third), fourth);
#else
        HashCode.Combine(first, second, third, fourth);
#endif
}
