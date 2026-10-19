namespace Wyzely.Functional.MaybeTests;

// MaybeConfiguration is process-wide, so tests that change it must not run at the same time as any other test.
[CollectionDefinition(Name, DisableParallelization = true)]
public static class ProcessWideConfiguration
{
    internal const string Name = "Process-wide configuration";
}
