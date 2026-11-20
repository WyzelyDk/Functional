namespace Wyzely.Functional.Internal;

/// <summary>Combines hash codes with <c>System.HashCode</c>.</summary>
internal static class HashCodeHelper
{
    /// <summary>Combines two hash codes.</summary>
    /// <param name="first">The first hash code.</param>
    /// <param name="second">The second hash code.</param>
    /// <returns>A hash code that depends on both values and on their order.</returns>
    internal static int Combine(int first, int second) => HashCode.Combine(first, second);

    /// <summary>Combines three hash codes.</summary>
    /// <param name="first">The first hash code.</param>
    /// <param name="second">The second hash code.</param>
    /// <param name="third">The third hash code.</param>
    /// <returns>A hash code that depends on all values and on their order.</returns>
    internal static int Combine(int first, int second, int third) => HashCode.Combine(first, second, third);

    /// <summary>Combines four hash codes.</summary>
    /// <param name="first">The first hash code.</param>
    /// <param name="second">The second hash code.</param>
    /// <param name="third">The third hash code.</param>
    /// <param name="fourth">The fourth hash code.</param>
    /// <returns>A hash code that depends on all values and on their order.</returns>
    internal static int Combine(int first, int second, int third, int fourth) => HashCode.Combine(first, second, third, fourth);
}
