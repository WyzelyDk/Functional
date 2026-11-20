namespace Wyzely.Functional.SharedTests;

// An abstract class with abstract, virtual, protected and internal members, for PublicApiDumpTests.
public abstract class SampleAbstract
{
    protected SampleAbstract()
    {
    }

    public abstract int Size { get; }

    public virtual string? Label { get; set; }

    public string Writable { get; set; } = string.Empty;

    public string InitOnly { get; init; } = string.Empty;

    public string ReadOnly { get; } = string.Empty;

    internal int Secret { get; set; }

    protected virtual void Hook() => throw new NotSupportedException();
}
