using Wyzely.Functional.CodeGeneration;

namespace Wyzely.Functional.SharedTests;

// The generator engine is shared by two libraries whose catalogs use different options, so each library exercises only half of the choices the
// emitters make. These tests drive the emitters with a synthetic profile and operation, so that every choice is checked in one place.
public sealed class EmitterOptionTests
{
    [Fact]
    internal void Done_renders_the_completed_task_of_the_kind_of_the_delegate_and_shares_the_task_of_none_only_when_the_profile_caches_it()
    {
        var withTask = Variant.All.First(static variant => variant.Receiver == Wrapper.None && variant.Delegate == Wrapper.Task);
        var withValueTask = Variant.All.First(static variant => variant.Receiver == Wrapper.None && variant.Delegate == Wrapper.ValueTask);
        var withoutWrapper = Variant.All.First(static variant => variant.Receiver == Wrapper.None && variant.Delegate == Wrapper.None);

        Assert.Multiple(
            () => Assert.Equal("MaybeTasks<int>.None", new BodyContext(withTask, "Maybe<int>", Profile(cachesNone: true)).Done("default(Maybe<int>)")),
            () => Assert.Equal("Task.FromResult(default(Maybe<int>))", new BodyContext(withTask, "Maybe<int>", Profile()).Done("default(Maybe<int>)")),
            () => Assert.Equal("Task.FromResult(value)", new BodyContext(withTask, "Maybe<int>", Profile(cachesNone: true)).Done("value")),
            () => Assert.Equal("new ValueTask<Maybe<int>>(value)", new BodyContext(withValueTask, "Maybe<int>", Profile()).Done("value")),
            () => Assert.Equal("value", new BodyContext(withoutWrapper, "Maybe<int>", Profile()).Done("value")));
    }

    [Theory]
    [InlineData(StateChange.ToNone, true, "MaybeTasks<T>.None")]
    [InlineData(StateChange.ToNone, false, "Task.FromResult(completed)")]
    [InlineData(StateChange.FromNone, true, "Task.FromResult(completed)")]
    [InlineData(StateChange.ToFailure, true, "Task.FromResult(completed)")]
    [InlineData(StateChange.Any, true, "completed.IsNone ? MaybeTasks<T>.None : Task.FromResult(completed)")]
    [InlineData(StateChange.Any, false, "Task.FromResult(completed)")]
    internal void A_task_receiver_hands_back_the_task_for_the_state_that_the_operation_can_produce(StateChange stateChange, bool cachesNone, string expected)
    {
        var variant = Variant.All.First(static candidate => candidate.Receiver == Wrapper.Task && candidate.Delegate == Wrapper.None);

        var text = new LibraryEmitter(Profile(cachesNone)).Emit(Probe(stateChange), variant, stubs: false);

        Assert.Contains($"return completed.HasSameStateAs(receiver.Result) ? receiver : {expected};", text, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(ZipKind.Maybe, "Maybe<(", "maybe")]
    [InlineData(ZipKind.Result, "Result<(", "result")]
    internal void Zip_emits_the_library_files_and_tests_of_its_kind(ZipKind kind, string returnType, string noun)
    {
        var emitter = new ZipEmitter(Profile(zip: kind), kind);

        var plain = emitter.EmitLibrary(arity: 2, selector: false, stubs: false);
        var withSelector = emitter.EmitLibrary(arity: 3, selector: true, stubs: false);
        var stubbed = emitter.EmitLibrary(arity: 2, selector: true, stubs: true);
        var tests = emitter.EmitTests(arity: GeneratorSettings.MaxZipArity);

        Assert.Multiple(
            () => Assert.Contains(returnType, plain, StringComparison.Ordinal),
            () => Assert.Contains(noun, plain, StringComparison.Ordinal),
            () => Assert.Contains("selector", withSelector, StringComparison.Ordinal),
            () => Assert.Contains("return default", stubbed, StringComparison.Ordinal),
            () => Assert.Contains(kind == ZipKind.Maybe ? "Maybe.Some(1)" : "Result.Success(1)", tests, StringComparison.Ordinal),
            () => Assert.EndsWith(".g.cs", emitter.LibraryPath(arity: 2, selector: false), StringComparison.Ordinal),
            () => Assert.NotEqual(emitter.LibraryPath(arity: 2, selector: false), emitter.LibraryPath(arity: 2, selector: true)),
            () => Assert.StartsWith("test/Library/", emitter.TestPath(arity: 2), StringComparison.Ordinal));
    }

    [Fact]
    internal void Zip_of_maybes_also_emits_the_file_for_a_selector_that_returns_a_nullable_value_type()
    {
        var emitter = new ZipEmitter(Profile(zip: ZipKind.Maybe), ZipKind.Maybe);

        var nullable = emitter.EmitLibrary(arity: 2, selector: true, stubs: false, nullableStruct: true);

        Assert.Multiple(
            () => Assert.Contains("where TResult : struct", nullable, StringComparison.Ordinal),
            () => Assert.NotEqual(emitter.LibraryPath(arity: 2, selector: true), emitter.LibraryPath(arity: 2, selector: true, nullableStruct: true)));
    }

    private static GeneratorProfile Profile(bool cachesNone = false, ZipKind? zip = null) => new(
        ToolNamespace: "Tool",
        ToolProject: "src/Tool",
        ExtensionsClass: "Extensions",
        ConfigurationClass: "Configuration",
        OptionsClass: "Options",
        ReceiverParameter: "receiver",
        LibraryDirectory: "src/Library",
        TestDirectory: "test/Library",
        TestNamespace: "Library.Tests",
        TestPreamble: [],
        Zip: zip,
        CachesCompletedNone: cachesNone);

    private static OperationSpec Probe(StateChange stateChange) => new()
    {
        Stem = "ProbeFromMaybe",
        Name = "Probe",
        TypeParameters = ["T"],
        TypeParameterDocs = [new("T", "The type of the value.")],
        Summary = "Probes the value.",
        Delegates = [new DelegateSpec("probe", ["T"], "bool", "The probe.")],
        ReturnType = "Maybe<T>",
        ReturnDescription = "The maybe.",
        Body = static _ => ["return default;"],
        KeepsValue = true,
        StateChange = stateChange,
        Tests = [],
        ReceiverType = "Maybe<T>",
        ReceiverTestType = "Maybe<int>",
        ReceiverDescription = "The maybe.",
    };
}
