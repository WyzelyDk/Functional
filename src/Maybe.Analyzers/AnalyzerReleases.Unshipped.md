; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
WYZ0202 | Design | Info | MaybeSuggestionsAnalyzer: use Any instead of Filter followed by IsSome
WYZ0203 | Design | Info | MaybeSuggestionsAnalyzer: use ValueOr instead of a conditional on IsSome
WYZ0204 | Design | Info | MaybeSuggestionsAnalyzer: use Maybe.From instead of a null check
