using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using Perfolizer.Horology;

// Usage: dotnet run -c Release --project benchmarks/Maybe.Benchmarks -- --filter "*SyncSuccess*"
// The in-process toolchain is used because the default one generates a project that inherits the strict analyzer settings of this repository.
// 5 warm-up and 10 measured iterations of 100 ms each: long enough for nanosecond-scale code, short enough to run the whole suite in minutes.
var job = Job.ShortRun
    .WithWarmupCount(5)
    .WithIterationCount(10)
    .WithIterationTime(TimeInterval.FromMilliseconds(100))
    .WithToolchain(InProcessEmitToolchain.Instance);

var config = DefaultConfig.Instance.AddJob(job);

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);
