namespace Wyzely.Functional;

/// <content>
/// The text of a result, used by the debugger and by logging.
/// </content>
public readonly partial struct Result<T>
{
    /// <summary>Describes the result as <c>Success(value)</c> or <c>Failure: message</c>. Sensitive error details are redacted.</summary>
    /// <returns>A short description of the state. It never throws.</returns>
    public override string ToString()
    {
        if (_error is not null)
        {
            return "Failure: " + _error.ToRedactedString();
        }

        return "Success(" + (_value is null ? "null" : _value.ToString()) + ")";
    }
}
