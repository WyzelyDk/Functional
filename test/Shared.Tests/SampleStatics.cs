using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Wyzely.Functional.SharedTests;

// A type with one example of each kind of method that PublicApiDump describes, for PublicApiDumpTests.
public static class SampleStatics
{
    public static string? Find(string? text, string name) => text is null ? null : name;

    public static T Some<T>(T value)
        where T : notnull => value;

    public static T Reference<T>()
        where T : class, new() => new();

    public static T Value<T>(T value)
        where T : struct => value;

    public static T Disposable<T>()
        where T : IDisposable, new() => new();

    public static T Anything<T>(T value) => value;

    public static int Twice(this int value) => value * 2;

    [OverloadResolutionPriority(1)]
    public static int Prefer(Func<int> work) => work();

    public static bool TryGet([NotNullWhen(true)] out string? value)
    {
        value = null;

        return false;
    }

    public static (int Index, string Name) Pair() => (1, "one");

    public static Task<(int Count, bool Done)> PairLater() => Task.FromResult((1, true));

    public static (IReadOnlyList<T> Values, int Count) GenericPair<T>() => ([], 0);

    public static int Defaults(int number = 5, string text = "a", bool flag = true, params int[] rest) => number + text.Length + (flag ? 1 : 0) + rest.Length;

    public static void Referenced(ref int value, in int input)
    {
        value = input;
    }

    public static int UsesHidden() => Hidden();

    internal static int Internal() => 1;

    private static int Hidden() => 1;
}
