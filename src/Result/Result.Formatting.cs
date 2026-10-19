namespace Wyzely.Functional;

/// <content>
/// The text of a result, used by the debugger and by logging.
/// </content>
public readonly partial struct Result
{
    /// <summary>Describes the result as <c>Success</c> or <c>Failure: message</c>. Sensitive error details are redacted.</summary>
    /// <returns>A short description of the state. It never throws.</returns>
    public override string ToString()
        => _error is null ? "Success" : "Failure: " + _error.ToRedactedString();
}
