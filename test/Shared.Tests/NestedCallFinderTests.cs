namespace Wyzely.Functional.SharedTests;

// The finder has to see each way of nesting a call and must not report what the rule allows, or the test of the repository proves nothing.
public sealed class NestedCallFinderTests
{
    [Theory]
    [InlineData("void M() { F(G(1)); }")]
    [InlineData("void M() { F(1, G(2), 3); }")]
    [InlineData("void M() { var x = F(a.B().C()); }")]
    [InlineData("void M() { F(new Foo(1)); }")]
    [InlineData("void M() { var x = new Foo(G(1)); }")]
    [InlineData("void M() { F($\"x: {G(1)}\"); }")]
    [InlineData("void M() { F(\"x: \" + G(1)); }")]
    [InlineData("void M() { F(a ? G(1) : 2); }")]
    [InlineData("async Task M() { F(await T()); }")]
    [InlineData("void M() => F(G(1));")]
    internal void A_call_in_the_arguments_of_another_call_is_reported(string member)
    {
        var found = NestedCallFinder.Find(Wrap(member));

        Assert.Single(found);
    }

    [Fact]
    internal void A_nesting_several_levels_deep_is_reported_once_for_the_outermost_call()
    {
        var found = NestedCallFinder.Find(Wrap("void M() { F(G(H(1))); }"));

        Assert.Equal(["F(G(H(1)))"], found.Select(static call => call.Text));
    }

    [Fact]
    internal void The_line_and_the_text_of_the_call_are_reported()
    {
        var found = NestedCallFinder.Find("class C\n{\n    void M()\n    {\n        F(\n            G(1));\n    }\n}\n");

        var call = Assert.Single(found);

        Assert.Multiple(
            () => Assert.Equal(5, call.Line),
            () => Assert.Equal("F( G(1))", call.Text));
    }

    [Theory]
    [InlineData("void M() { F(x); }")]
    [InlineData("void M() { F(1, \"text\", null); }")]
    [InlineData("void M() { F(a.B); }")]
    [InlineData("void M() { F(nameof(M)); }")]
    [InlineData("void M() { Guard.NotNull(x, nameof(x)); }")]
    [InlineData("void M() { F(x => G(x)); }")]
    [InlineData("void M() { F(static x => G(x)); }")]
    [InlineData("void M() { F(G); }")]
    [InlineData("void M() { F(new Foo()); }")]
    [InlineData("void M() { F(new()); }")]
    [InlineData("void M() { F([1, 2]); }")]
    [InlineData("void M() { var x = a.B().C().D(); }")]
    [InlineData("void M() { var x = await a.B().C(); }")]
    [InlineData("void M() { var y = G(1); F(y); }")]
    [InlineData("void M() { F(a ? 1 : 2); }")]
    [InlineData("void M() { var x = (int)G(1); }")]
    [InlineData("static readonly Foo Field = new Foo(G(1));")]
    [InlineData("public int Property { get; } = F(G(1));")]
    [InlineData("[Obsolete(F(G(1)))] void M() { }")]
    [InlineData("void M(int x = F(G(1))) { }")]
    [InlineData("C() : this(F(G(1))) { }")]
    internal void What_the_rule_allows_and_a_place_that_cannot_hold_a_local_are_not_reported(string member)
    {
        var found = NestedCallFinder.Find(Wrap(member));

        Assert.Empty(found);
    }

    [Fact]
    internal void A_call_inside_the_expression_of_a_lambda_that_is_an_argument_is_checked_like_any_other()
    {
        var found = NestedCallFinder.Find(Wrap("void M() { F(static x => G(H(x))); }"));

        Assert.Equal(["G(H(x))"], found.Select(static call => call.Text));
    }

    [Fact]
    internal void A_call_inside_the_block_of_a_lambda_that_is_an_argument_is_checked_like_any_other()
    {
        var found = NestedCallFinder.Find(Wrap("void M() { F(x => { return G(H(x)); }); }"));

        Assert.Equal(["G(H(x))"], found.Select(static call => call.Text));
    }

    [Fact]
    internal void The_base_call_of_a_primary_constructor_cannot_hold_a_local()
    {
        var found = NestedCallFinder.Find("class D(int a) : B(F(G(a))) { }");

        Assert.Empty(found);
    }

    [Fact]
    internal void Code_behind_a_directive_for_either_target_is_checked()
    {
        const string source =
            "class C\n{\n#if NET11_0_OR_GREATER\n    void M() { F(G(1)); }\n#else\n" +
            "    void N() { F(G(2)); }\n#endif\n}\n";

        var found = NestedCallFinder.Find(source);

        Assert.Equal([4, 6], found.Select(static call => call.Line));
    }

    private static string Wrap(string member) => "class C\n{\n    " + member + "\n}\n";
}
