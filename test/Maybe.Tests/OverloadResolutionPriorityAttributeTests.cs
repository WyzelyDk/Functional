#if !NET9_0_OR_GREATER

using System.Runtime.CompilerServices;

namespace Wyzely.Functional.MaybeTests;

// The attribute is declared inside the library on the targets that lack it, so its test belongs to the project that compiles the library.
public sealed class OverloadResolutionPriorityAttributeTests
{
    [Fact]
    internal void The_attribute_keeps_the_priority_it_was_given()
    {
        var attribute = new OverloadResolutionPriorityAttribute(3);

        Assert.Equal(3, attribute.Priority);
    }
}

#endif
