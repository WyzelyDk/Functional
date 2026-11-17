using Wyzely.Functional.ArgumentTesting;

namespace Wyzely.Functional.ResultTests;

// The probe has to see the ways in which a method can fail to reject a null argument at the call, or the tests that use it prove nothing.
public sealed class NullArgumentProbeTests
{
    [Fact]
    internal void A_method_that_throws_at_the_call_for_the_right_argument_is_not_reported()
    {
        var report = NullArgumentProbe.Probe([typeof(Rejecting)]);

        Assert.Multiple(
            () => Assert.Equal(2, report.Probed),
            () => Assert.Empty(report.Violations),
            () => Assert.Empty(report.Unsupported));
    }

    [Fact]
    internal void An_argument_that_is_annotated_as_nullable_is_not_probed()
    {
        var report = NullArgumentProbe.Probe([typeof(Accepting)]);

        Assert.Equal(0, report.Probed);
    }

    [Fact]
    internal void A_method_that_names_another_argument_in_the_exception_is_reported()
    {
        var report = NullArgumentProbe.Probe([typeof(WrongName)]);

        Assert.Equal(["WrongName.Check(String), parameter 'text': threw ArgumentNullException for 'other' at the call"], report.Violations);
    }

    [Fact]
    internal void A_method_that_throws_only_when_the_returned_task_is_awaited_is_reported()
    {
        var report = NullArgumentProbe.Probe([typeof(Deferring)]);

        Assert.Equal(["Deferring.CheckAsync(Task), parameter 'task': threw only when the returned task was awaited, not at the call"], report.Violations);
    }

    [Fact]
    internal void A_method_that_returns_without_throwing_is_reported()
    {
        var report = NullArgumentProbe.Probe([typeof(Silent)]);

        Assert.Equal(["Silent.Check(String), parameter 'text': returned String without throwing"], report.Violations);
    }

    [Fact]
    internal void An_asynchronous_stream_that_checks_when_it_is_read_is_reported()
    {
        var report = NullArgumentProbe.Probe([typeof(Streaming)]);

        Assert.Single(report.Violations, static violation => violation.Contains("without throwing", StringComparison.Ordinal));
    }

    [Fact]
    internal void A_generic_method_is_probed_with_type_arguments_that_satisfy_its_constraints()
    {
        var report = NullArgumentProbe.Probe([typeof(Generic)]);

        Assert.Multiple(
            () => Assert.Equal(2, report.Probed),
            () => Assert.Empty(report.Violations),
            () => Assert.Empty(report.Unsupported));
    }

    [Fact]
    internal void A_constructor_that_does_not_validate_is_reported_and_one_that_does_is_not()
    {
        var report = NullArgumentProbe.Probe([typeof(LooseConstructor), typeof(StrictConstructor)]);

        Assert.Equal(["LooseConstructor..ctor(String), parameter 'text': returned LooseConstructor without throwing"], report.Violations);
    }

    [Fact]
    internal void A_property_setter_that_accepts_null_is_reported_and_one_that_rejects_it_is_not()
    {
        var report = NullArgumentProbe.Probe([typeof(LooseSetter), typeof(StrictSetter)]);

        Assert.Equal(["LooseSetter.set_Text(String), parameter 'value': returned nothing without throwing"], report.Violations);
    }

    [Fact]
    internal void An_instance_method_of_a_generic_type_is_probed()
    {
        var report = NullArgumentProbe.Probe([typeof(Holder<>)]);

        Assert.Equal(["Holder<T>.Use(Func<T>), parameter 'source': returned nothing without throwing"], report.Violations);
    }

    [Fact]
    internal void A_conversion_operator_that_does_not_validate_is_reported_and_one_that_does_is_not()
    {
        var report = NullArgumentProbe.Probe([typeof(LooseConversion), typeof(StrictConversion)]);

        Assert.Equal(["LooseConversion.op_Implicit(String), parameter 'text': returned LooseConversion without throwing"], report.Violations);
    }

    public static class Rejecting
    {
        public static Task CheckAsync(Task task, string text)
        {
            ArgumentNullException.ThrowIfNull(task);
            ArgumentNullException.ThrowIfNull(text);

            return Task.CompletedTask;
        }
    }

    public static class Accepting
    {
        public static string Check(string? text) => text ?? "none";
    }

    public static class WrongName
    {
        private static readonly string Other = "other";

        public static void Check(string text) => throw new ArgumentNullException(Other, text);
    }

    public static class Deferring
    {
        public static async Task CheckAsync(Task task)
        {
            ArgumentNullException.ThrowIfNull(task);

            await task;
        }
    }

    public static class Silent
    {
        public static string Check(string text) => text ?? string.Empty;
    }

    public static class Streaming
    {
        public static IAsyncEnumerable<int> Read(Func<int> source) => ReadCore(source);

        private static async IAsyncEnumerable<int> ReadCore(Func<int> source)
        {
            await Task.Yield();

            yield return source();
        }
    }

    public static class Generic
    {
        public static void Check<T, TStruct>(Func<T> source, Func<TStruct, T> map)
            where TStruct : struct
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(map);
        }
    }

    public sealed class LooseConstructor(string text)
    {
        public string Text { get; } = text;
    }

    public sealed class StrictConstructor(string text)
    {
        public string Text { get; } = text ?? throw new ArgumentNullException(nameof(text));
    }

    public sealed class LooseSetter
    {
        public string Text { get; set; } = string.Empty;
    }

    public sealed class StrictSetter
    {
        public string Text
        {
            get;
            set => field = value ?? throw new ArgumentNullException(nameof(value));
        } = string.Empty;
    }

    public sealed class LooseConversion
    {
        private LooseConversion(string text) => Text = text;

        public string Text { get; }

        public static implicit operator LooseConversion(string text) => new(text);
    }

    public sealed class StrictConversion
    {
        private StrictConversion(string text) => Text = text;

        public string Text { get; }

        public static implicit operator StrictConversion(string text)
        {
            ArgumentNullException.ThrowIfNull(text);

            return new(text);
        }
    }

    public sealed class Holder<T>
    {
        public Func<T>? Source { get; private set; }

        public void Use(Func<T> source) => Source = source;
    }
}
