; Shipped analyzer releases
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

## Release 0.1.0

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
WYZ0201 | Usage | Warning | MaybeUsageAnalyzer: check that a Maybe has a value before calling GetValue()

## Release 0.2.0

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
WYZ0202 | Design | Info | MaybeSuggestionsAnalyzer: use Any instead of Filter followed by IsSome
WYZ0203 | Design | Info | MaybeSuggestionsAnalyzer: use ValueOr instead of a conditional on IsSome
WYZ0204 | Design | Info | MaybeSuggestionsAnalyzer: use Maybe.From instead of a null check

## Release 0.3.0

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
WYZ0205 | Design | Info | MaybeSuggestionsAnalyzer: use ValueOr instead of Match that returns the value or a fallback
WYZ0206 | Design | Info | MaybeSuggestionsAnalyzer: use Map instead of Bind that only wraps the value in Some
WYZ0207 | Design | Info | MaybeSuggestionsAnalyzer: remove a step that does not change the maybe
WYZ0208 | Design | Info | MaybeSuggestionsAnalyzer: pass a function to ValueOr when the fallback does work
