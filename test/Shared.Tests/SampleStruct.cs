namespace Wyzely.Functional.SharedTests;

// A readonly struct with a constrained type parameter and an init-only property, for PublicApiDumpTests.
public readonly struct SampleStruct<T>
    where T : notnull
{
    public T Value { get; init; }
}
