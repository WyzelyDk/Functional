using Wyzely.Functional.ApiSurface;

namespace Wyzely.Functional.SharedTests;

// The public API dump is what the approval tests of the packages compare, so what it shows and what it leaves out is tested here, on small samples.
public sealed class PublicApiDumpTests
{
    private static readonly string Statics = PublicApiDump.OfTypes([typeof(SampleStatics)]);

    [Fact]
    internal void A_type_line_names_the_kind_and_the_members_follow_sorted_with_the_names_of_their_parameters()
    {
        var lines = Statics.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var members = lines.Skip(1).ToList();

        Assert.Multiple(
            () => Assert.Equal("static class Wyzely.Functional.SharedTests.SampleStatics", lines[0]),
            () => Assert.Equal(members.Order(StringComparer.Ordinal), members),
            () => Assert.Contains("  static string? Find(string? text, string name)", lines));
    }

    [Fact]
    internal void Constraints_are_shown_and_notnull_is_one_of_them_while_an_unconstrained_type_parameter_has_none()
    {
        Assert.Multiple(
            () => Assert.Contains("  static T Some<T>(T value) where T : notnull", Statics),
            () => Assert.Contains("  static T Reference<T>() where T : class, new()", Statics),
            () => Assert.Contains("  static T Value<T>(T value) where T : struct", Statics),
            () => Assert.Contains("  static T Disposable<T>() where T : System.IDisposable, new()", Statics),
            () => Assert.Contains("  static T? Anything<T>(T? value)\n", Statics));
    }

    [Fact]
    internal void An_extension_method_shows_this_on_its_first_parameter()
    {
        Assert.Contains("  static int Twice(this int value)", Statics);
    }

    [Fact]
    internal void The_attributes_that_change_binding_and_flow_are_shown_and_the_pseudo_attributes_of_the_compiler_are_not()
    {
        Assert.Multiple(
            () => Assert.Contains("  [OverloadResolutionPriority(1)] static int Prefer(System.Func<int> work)", Statics),
            () => Assert.Contains("  static bool TryGet([NotNullWhen(true)] out string? value)", Statics),
            () => Assert.DoesNotContain("Nullable", Statics),
            () => Assert.DoesNotContain("Out]", Statics),
            () => Assert.DoesNotContain("Optional", Statics));
    }

    [Fact]
    internal void The_names_of_tuple_elements_are_shown_also_inside_other_generic_types()
    {
        Assert.Multiple(
            () => Assert.Contains("  static (int Index, string Name) Pair()", Statics),
            () => Assert.Contains("  static System.Threading.Tasks.Task<(int Count, bool Done)> PairLater()", Statics),
            () => Assert.Contains("  static (System.Collections.Generic.IReadOnlyList<T> Values, int Count) GenericPair<T>()", Statics));
    }

    [Fact]
    internal void Default_values_params_and_the_modifiers_of_a_parameter_are_shown()
    {
        Assert.Multiple(
            () => Assert.Contains("  static int Defaults(int number = 5, string text = \"a\", bool flag = true, params int[] rest)", Statics),
            () => Assert.Contains("  static void Referenced(ref int value, in int input)", Statics));
    }

    [Fact]
    internal void A_member_that_a_caller_cannot_see_is_left_out()
    {
        Assert.Multiple(
            () => Assert.DoesNotContain(" Hidden(", Statics),
            () => Assert.DoesNotContain(" Internal(", Statics),
            () => Assert.DoesNotContain("Secret", PublicApiDump.OfTypes([typeof(SampleAbstract)])));
    }

    [Fact]
    internal void Properties_show_their_accessors_and_a_protected_member_is_part_of_the_api()
    {
        var text = PublicApiDump.OfTypes([typeof(SampleAbstract)]);

        Assert.Multiple(
            () => Assert.Contains("abstract class Wyzely.Functional.SharedTests.SampleAbstract\n", text),
            () => Assert.Contains("  abstract property int Size { get; }", text),
            () => Assert.Contains("  property string Writable { get; set; }", text),
            () => Assert.Contains("  property string InitOnly { get; init; }", text),
            () => Assert.Contains("  property string ReadOnly { get; }", text),
            () => Assert.Contains("  virtual property string? Label { get; set; }", text),
            () => Assert.Contains("  virtual void Hook()", text),
            () => Assert.Contains("  ctor()", text));
    }

    [Fact]
    internal void Overrides_events_constants_and_interfaces_are_described()
    {
        var text = PublicApiDump.OfTypes([typeof(SampleDerived)]);

        Assert.Multiple(
            () => Assert.Contains(
                "sealed class Wyzely.Functional.SharedTests.SampleDerived : " +
                "Wyzely.Functional.SharedTests.ISample, Wyzely.Functional.SharedTests.SampleAbstract\n",
                text),
            () => Assert.Contains("  const int Limit", text),
            () => Assert.Contains("  static readonly string Name", text),
            () => Assert.Contains("  event System.EventHandler? Changed", text),
            () => Assert.Contains("  override property string? Label { get; set; }", text),
            () => Assert.Contains("  override property int Size { get; }", text),
            () => Assert.Contains("  int Read(string key)", text));
    }

    [Fact]
    internal void Structs_interfaces_and_enums_say_what_they_are()
    {
        var structText = PublicApiDump.OfTypes([typeof(SampleStruct<>)]);
        var interfaceText = PublicApiDump.OfTypes([typeof(ISample)]);
        var enumText = PublicApiDump.OfTypes([typeof(SampleKind)]);

        Assert.Multiple(
            () => Assert.StartsWith("readonly struct Wyzely.Functional.SharedTests.SampleStruct<T> where T : notnull\n", structText),
            () => Assert.Contains("  property T Value { get; init; }", structText),
            () => Assert.StartsWith("interface Wyzely.Functional.SharedTests.ISample\n", interfaceText),
            () => Assert.Contains("  int Read(string key)", interfaceText),
            () => Assert.StartsWith("enum Wyzely.Functional.SharedTests.SampleKind\n", enumText),
            () => Assert.Contains("  First = 1", enumText),
            () => Assert.Contains("  Second = 2", enumText));
    }
}
