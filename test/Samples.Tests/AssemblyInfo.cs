using Xunit.Sdk;
using Xunit.v3;

// Every example may change and restore process-wide settings (separators, redaction, ContinueOnCapturedContext), so no test of this
// assembly may run at the same time as another one. Doing it for the assembly means a new test class cannot forget to opt in.
[assembly: Parallelization(Mode = ParallelMode.None)]
