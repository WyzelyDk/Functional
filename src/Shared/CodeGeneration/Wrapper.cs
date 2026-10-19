namespace Wyzely.Functional.CodeGeneration;

/// <summary>How a receiver or a delegate is wrapped: not at all (synchronous), in a Task, or in a ValueTask.</summary>
internal enum Wrapper
{
    None,
    Task,
    ValueTask,
}

internal static class WrapperExtensions
{
    /// <summary>Wraps a type: <c>T</c>, <c>Task&lt;T&gt;</c> or <c>ValueTask&lt;T&gt;</c>.</summary>
    internal static string Wrap(this Wrapper wrapper, string inner) => wrapper switch
    {
        Wrapper.None => inner,
        Wrapper.Task => $"Task<{inner}>",
        _ => $"ValueTask<{inner}>",
    };

    /// <summary>The return type of an operation without a value: <c>void</c>, <c>Task</c> or <c>ValueTask</c>.</summary>
    internal static string WrapVoid(this Wrapper wrapper) => wrapper switch
    {
        Wrapper.None => "void",
        Wrapper.Task => "Task",
        _ => "ValueTask",
    };

    /// <summary>A lower-case word for test names.</summary>
    internal static string Slug(this Wrapper wrapper) => wrapper switch
    {
        Wrapper.None => "sync",
        Wrapper.Task => "task",
        _ => "value_task",
    };

    /// <summary>A readable name for documentation.</summary>
    internal static string Label(this Wrapper wrapper) => wrapper switch
    {
        Wrapper.None => "synchronous",
        Wrapper.Task => "<see cref=\"Task\"/>",
        _ => "<see cref=\"ValueTask\"/>",
    };
}
