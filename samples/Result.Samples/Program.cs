// Runs every example of the Wyzely.Functional.Result samples, one section each.
// Start with BasicsExample and read the files in this order: each one is written to be read, and every
// example explains the scenario and why it is written the way it is.
using Wyzely.Functional.ResultSamples;

var output = Console.Out;

Section("1. Basics: create and read a result", BasicsExample.Run);
Section("2. Errors in depth", ErrorsExample.Run);
Section("3. The railway", RailwayExample.Run);
Section("4. Async: Task and ValueTask in one chain", AsyncExample.Run);
Section("5. Validation and combining", CombiningExample.Run);
Section("6. Exceptions at the boundary", ExceptionsExample.Run);
Section("7. Parallel", ParallelExample.Run);
Section("8. Configuration", ConfigurationExample.Run);
Section("9. Untyped Result and Result<T>", UntypedExample.Run);
Section("10. C# 15 unions", UnionsExample.Run);

return 0;

void Section(string title, Action<TextWriter> run)
{
    output.WriteLine();
    output.WriteLine($"=== {title} ===");
    run(output);
}
