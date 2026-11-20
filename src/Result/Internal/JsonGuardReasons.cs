namespace Wyzely.Functional.Internal;

/// <summary>Why the types of a result have no JSON form, as the sentences that a refused serialization explains itself with.</summary>
internal static class JsonGuardReasons
{
    /// <summary>The reason for a result: its value is not written, and a failure would be read back as a success.</summary>
    internal const string Result = "its value is not written, and a failure would be read back as a success.";

    /// <summary>The reason for an error and its parts: a serializer writes the values of sensitive details in plain text.</summary>
    internal const string SensitiveDetails = "it can carry sensitive details, which a serializer would write in plain text.";

    /// <summary>The reason for an error kind: it would be read back without its name.</summary>
    internal const string ErrorKind = "it would be read back without its name.";
}
