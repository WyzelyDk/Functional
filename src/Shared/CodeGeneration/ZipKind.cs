namespace Wyzely.Functional.CodeGeneration;

/// <summary>Which kind of value <c>Zip</c> combines. It decides how a missing value or a failure is handled.</summary>
internal enum ZipKind
{
    /// <summary>A maybe: the combination has a value only when every maybe has one.</summary>
    Maybe,

    /// <summary>A result: the combination succeeds only when every result succeeded, and otherwise reports every error.</summary>
    Result,
}
