using Wyzely.Functional.ApiSurface;

namespace Wyzely.Functional.ResultTests;

// The public API is the contract of the package: a change of it must be a decision that shows in the review (see test/ApiSurface/Approved).
public sealed class PublicApiTests
{
    [Fact]
    internal void The_public_api_is_the_approved_one()
    {
        PublicApiApproval.Check(typeof(Result).Assembly);
    }
}
