namespace Wyzely.Functional.CodeGeneration;

/// <summary>The settings of the generator that apply to every library. Each has exactly one definition, here.</summary>
internal static class GeneratorSettings
{
    /// <summary>
    /// The largest number of values that <c>Zip</c> combines. Overloads are generated for every count from 2 up to this number, for
    /// both <c>Maybe</c> and <c>Result</c>, together with their tests. Raise it here and regenerate; nothing else mentions the number.
    /// </summary>
    internal const int MaxZipArity = 5;

    /// <summary>The widest line of generated code. Longer lines are wrapped at a sensible place (see <c>LineWrapper</c>).</summary>
    internal const int MaxLineWidth = 160;

    /// <summary>The width that wrapped documentation text is filled to, a little below the limit so that it reads well.</summary>
    internal const int PreferredDocWidth = 140;
}
