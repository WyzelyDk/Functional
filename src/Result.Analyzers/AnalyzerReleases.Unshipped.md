; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
WYZ0118 | Design | Info | ResultSuggestionsAnalyzer: use MapError instead of OrElse that only returns an error
WYZ0119 | Design | Info | ResultSuggestionsAnalyzer: use ValueOr instead of a conditional on the state of a result
WYZ0120 | Design | Info | ResultSuggestionsAnalyzer: pass a function to ValueOr when the fallback does work
