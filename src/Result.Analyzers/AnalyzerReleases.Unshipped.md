; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
WYZ0103 | Design | Info | ResultSuggestionsAnalyzer: use BindIf instead of a condition inside Bind
WYZ0104 | Design | Info | ResultSuggestionsAnalyzer: use MapIf instead of a condition inside Map
WYZ0105 | Design | Info | ResultSuggestionsAnalyzer: use EnsureNot instead of Ensure with a negated condition
WYZ0106 | Design | Info | ResultSuggestionsAnalyzer: use EnsureNotNull instead of a null check inside Bind
WYZ0107 | Design | Info | ResultSuggestionsAnalyzer: use BindZip to keep the earlier value
WYZ0108 | Design | Info | ResultSuggestionsAnalyzer: use ConvertFailure to pass a failure on as another type
WYZ0109 | Design | Info | ResultSuggestionsAnalyzer: use SuccessIf or FailureIf instead of a conditional expression
WYZ0110 | Design | Info | ResultSuggestionsAnalyzer: use MapTry instead of Result.Try inside Bind
WYZ0111 | Usage | Warning | ResultMessageAnalyzer: do not interpolate values into the message of an error
WYZ0112 | Design | Info | ResultSuggestionsAnalyzer: use Map instead of Bind that only wraps the value in Result.Success
WYZ0113 | Design | Info | ResultSuggestionsAnalyzer: use Tap instead of Map that returns the value it was given
WYZ0114 | Design | Info | ResultSuggestionsAnalyzer: use ValueOr instead of Match that returns the value or a fallback
WYZ0115 | Design | Info | ResultSuggestionsAnalyzer: remove a step that does not change the result
WYZ0116 | Design | Info | ResultSuggestionsAnalyzer: pass the message to Result.Failure instead of creating the error
WYZ0117 | Design | Info | ResultSuggestionsAnalyzer: use Flatten instead of Bind that returns the nested result
