; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
WYZ0205 | Design | Info | MaybeSuggestionsAnalyzer: use ValueOr instead of Match that returns the value or a fallback
WYZ0206 | Design | Info | MaybeSuggestionsAnalyzer: use Map instead of Bind that only wraps the value in Some
WYZ0207 | Design | Info | MaybeSuggestionsAnalyzer: remove a step that does not change the maybe
WYZ0208 | Design | Info | MaybeSuggestionsAnalyzer: pass a function to ValueOr when the fallback does work
