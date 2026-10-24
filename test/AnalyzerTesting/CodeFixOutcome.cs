namespace Wyzely.Functional.AnalyzerTesting;

// The outcome of asking a code fix provider for a fix: the code after applying it, or null when the provider offered nothing,
// and the ids of the diagnostics that remain afterwards.
internal sealed record CodeFixOutcome(string? FixedSource, IReadOnlyList<string> RemainingIds);
