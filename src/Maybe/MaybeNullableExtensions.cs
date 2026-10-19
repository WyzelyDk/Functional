namespace Wyzely.Functional;

/// <summary>Conversion from a <see cref="Maybe{T}"/> of a value type to a <see cref="Nullable{T}"/>.</summary>
public static class MaybeNullableExtensions
{
    /// <summary>Converts a maybe of a value type to a nullable value.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="maybe">The maybe to convert.</param>
    /// <returns>The value, or <see langword="null"/> when the maybe has none.</returns>
    public static T? ToNullable<T>(this Maybe<T> maybe)
        where T : struct => maybe.Unpack(out var value) ? value : null;
}
