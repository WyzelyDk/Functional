namespace Wyzely.Functional.SharedTests;

// The finder has to see each way of writing a lambda that is too big, and must not report what the rule allows, or the test of the repository proves nothing.
public sealed class NamedLambdaFinderTests
{
    [Theory]
    [InlineData("void M() { F(x => { return G(x); }); }")]
    [InlineData("void M() { F((x, y) => { return G(x, y); }); }")]
    [InlineData("void M() { F(() => { G(); }); }")]
    [InlineData("void M() { F(static x => { return x + 1; }); }")]
    [InlineData("void M() { F(async x => { await T(x); }); }")]
    [InlineData("void M() { F(delegate { G(); }); }")]
    [InlineData("void M() { F(delegate (int x) { return x; }); }")]
    [InlineData("void M() { Func<int, int> f = x => { return x; }; }")]
    [InlineData("Func<int, int> field = x => { return x; };")]
    internal void A_lambda_with_a_block_body_is_reported(string member)
    {
        var found = NamedLambdaFinder.Find(Wrap(member));

        var problem = Assert.Single(found);

        Assert.Contains("block body", problem.Reason, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("void M() { F(x => G(H(x))); }")]
    [InlineData("void M() { F(x => G(x).H()); }")]
    [InlineData("void M() { F((a, b) => G(a, b).H()); }")]
    [InlineData("void M() { F(x => G(x) && H(x)); }")]
    [InlineData("void M() { F(x => x.A().B()); }")]
    [InlineData("void M() { F(x => x.A().B().C()); }")]
    [InlineData("void M() { Func<int, int> f = x => G(x).H(); }")]
    internal void A_lambda_that_calls_two_or_more_times_on_one_line_is_reported(string member)
    {
        var found = NamedLambdaFinder.Find(Wrap(member));

        var problem = Assert.Single(found);

        Assert.Contains("two or more times", problem.Reason, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("void M() { F(x => x.Name); }")]
    [InlineData("void M() { F(x => x.A.B); }")]
    [InlineData("void M() { F(() => 5); }")]
    [InlineData("void M() { F(static x => x > 3); }")]
    [InlineData("void M() { F(x => !x.IsNone); }")]
    [InlineData("void M() { F(x => G(x)); }")]
    [InlineData("void M() { F(x => x.A()); }")]
    [InlineData("void M() { F(x => x.A.B(1)); }")]
    [InlineData("void M() { F(x => new Foo(x)); }")]
    [InlineData("void M() { F(x => x.Value = false); }")]
    [InlineData("void M() { F(x => nameof(x)); }")]
    [InlineData("void M() { F(G); }")]
    [InlineData("void M() { F(a => a ? G(1) : H(2)); }")]
    [InlineData("void M() { F(a => a is Foo { Bar: 1 } f && G(f) && H(f)); }")]
    [InlineData("void M() { F(a => a switch { 1 => G(1), _ => H(2) }); }")]
    [InlineData("void M() { F(x => G(x)); F(y => y.Z()); }")]
    [InlineData("void M() { F(x => [G(x), H(x), I(x)]); }")]
    [InlineData("void M() { F(x => new[] { G(x), H(x) }); }")]
    [InlineData("void M() { F(x => (G(x), H(x))); }")]
    [InlineData("void M() { F(x => $\"{G(x)} {H(x)}\"); }")]
    [InlineData("void M() { F(x => x.A()\n        .B()); }")]
    [InlineData("void M() { F(x => x\n        .A()\n        .B()\n        .C()); }")]
    [InlineData("void M() { F(x => G(x)\n        .H()); }")]
    [InlineData("void M() { F(x => x.A.B()\n        .C()); }")]
    internal void What_the_rule_allows_and_a_chain_with_a_line_per_dot_are_not_reported(string member)
    {
        var found = NamedLambdaFinder.Find(Wrap(member));

        Assert.Empty(found);
    }

    [Theory]
    [InlineData("void M() { F(x => x.A().B()\n        .C()); }")]
    [InlineData("void M() { F(x => G(x).H()\n        .I()); }")]
    [InlineData("void M() { F(x => G(x\n        .A()).B()); }")]
    internal void A_chain_in_which_a_dot_after_the_first_call_stays_on_the_line_of_the_call_before_is_reported(string member)
    {
        var found = NamedLambdaFinder.Find(Wrap(member));

        Assert.Single(found);
    }

    [Fact]
    internal void The_line_the_reason_and_the_text_are_reported()
    {
        var found = NamedLambdaFinder.Find("class C\n{\n    void M()\n    {\n        F(x =>\n        {\n            return G(x);\n        });\n    }\n}\n");

        var problem = Assert.Single(found);

        Assert.Multiple(
            () => Assert.Equal(5, problem.Line),
            () => Assert.Contains("block body", problem.Reason, StringComparison.Ordinal),
            () => Assert.Equal("x => { return G(x); }", problem.Text));
    }

    [Fact]
    internal void A_lambda_inside_a_lambda_is_checked_on_its_own()
    {
        var found = NamedLambdaFinder.Find(Wrap("void M() { F(x => G(y => { return y; })); }"));

        var problem = Assert.Single(found);

        Assert.Equal("y => { return y; }", problem.Text);
    }

    [Fact]
    internal void Code_behind_a_directive_for_either_target_is_checked()
    {
        const string source =
            "class C\n{\n#if NET11_0_OR_GREATER\n    void M() { F(x => { return x; }); }\n#else\n" +
            "    void N() { F(x => { return x; }); }\n#endif\n}\n";

        var found = NamedLambdaFinder.Find(source);

        Assert.Equal([4, 6], found.Select(static problem => problem.Line));
    }

    private static string Wrap(string member) => "class C\n{\n    " + member + "\n}\n";
}
