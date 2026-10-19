using Wyzely.Functional.MaybeSamples;

// Runs every Maybe example, each under a heading. Run it with:
//     dotnet run --project samples/Maybe.Samples -c Release
// The examples are in one file per theme, in the order of the documentation: read them top to bottom.
var output = Console.Out;

Section("1. Basics: create, read, leave", BasicsExample.Run);
Section("2. The railway: Map, Bind, Filter, Tap, Or, Match", RailwayExample.Run);
Section("3. Async: nine combinations, one name", AsyncExample.Run);
Section("4. Collections and dictionaries", CollectionsExample.Run);
Section("5. LINQ query syntax, Zip and Flatten", LinqExample.Run);
Section("6. Nullable reference and value types", NullabilityExample.Run);
Section("7. Equality, ordering and formatting", EqualityExample.Run);
Section("8. Configuration", ConfigurationExample.Run);
#if NET11_0_OR_GREATER
Section("9. C# 15 union switch (net11.0)", UnionExample.Run);
#endif

return 0;

void Section(string title, Action<TextWriter> example)
{
    output.WriteLine();
    output.WriteLine($"=== {title} ===");
    example(output);
}
