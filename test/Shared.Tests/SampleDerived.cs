namespace Wyzely.Functional.SharedTests;

// A sealed class with overrides, an event, a constant, a static readonly field and an interface, for PublicApiDumpTests.
public sealed class SampleDerived : SampleAbstract, ISample
{
    public const int Limit = 10;

    public static readonly string Name = "name";

    public event EventHandler? Changed;

    public override int Size => 1;

    public override string? Label { get; set; }

    public int Read(string key) => key.Length;

    public void Raise() => Changed?.Invoke(this, EventArgs.Empty);
}
