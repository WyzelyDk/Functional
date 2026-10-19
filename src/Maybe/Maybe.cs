namespace Wyzely.Functional;

/// <summary>
/// Creates <see cref="Maybe{T}"/> values: <see cref="Some{T}(T)"/>, <see cref="None{T}()"/>, <see cref="From{T}(T)"/> for values that
/// may be <see langword="null"/>, and the conditional factories.
/// </summary>
/// <remarks>
/// The factories live in a static class, so that <c>Maybe.Some(5)</c> can infer the type of the value. The members are in
/// <c>Maybe.Factories.cs</c>.
/// </remarks>
public static partial class Maybe
{
}
