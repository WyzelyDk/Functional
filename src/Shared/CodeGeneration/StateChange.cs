namespace Wyzely.Functional.CodeGeneration;

/// <summary>
/// How an operation that keeps the value of its receiver can change the state of the receiver (a maybe that has a value or none, a result
/// that succeeded or failed). The generated code for a completed task only contains the outcomes that can happen.
/// </summary>
internal enum StateChange
{
    /// <summary>The operation can leave the state as it is or move it to the other state.</summary>
    Any,

    /// <summary>The operation never changes the state: it returns the receiver as it was.</summary>
    Never,

    /// <summary>The operation can only move a maybe that has a value to none.</summary>
    ToNone,

    /// <summary>The operation can only move a maybe that has no value to one that has a value.</summary>
    FromNone,

    /// <summary>The operation can only move a result that succeeded to a failure.</summary>
    ToFailure,
}
