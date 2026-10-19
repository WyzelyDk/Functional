using Wyzely.Functional.FunctionalSamples;

// Runs every example of the umbrella package, one section each. Run it with:
//   dotnet run --project samples/Functional.Samples
Section("1. From absent to failed (Maybe.ToResult)", AbsentToFailedExample.Run);
Section("2. From failed to absent (Result.ToMaybe)", FailedToAbsentExample.Run);
Section("3. A full async workflow in one chain", AsyncWorkflowExample.Run);
Section("4. Collections and results together", CollectionsExample.Run);
Section("5. Configuration across the two packages", ConfigurationExample.Run);
Section("6. Analyzers through the umbrella package", AnalyzersExample.Run);
Section("7. Putting it together: order checkout", CheckoutExample.Run);

return 0;

static void Section(string title, Action<TextWriter> example)
{
    Console.WriteLine();
    Console.WriteLine($"=== {title} ===");
    example(Console.Out);
}
