namespace Wyzely.Functional.Tests;

// MaybeConfiguration and ResultConfiguration are process-wide, so tests that change them must not run at the same time as any other test.
[CollectionDefinition(Name, DisableParallelization = true)]
public static class ProcessWideConfiguration
{
    internal const string Name = "Process-wide configuration";
}
