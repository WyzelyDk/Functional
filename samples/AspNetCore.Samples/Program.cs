// Runs the ASP.NET Core recipe: an Error becomes a problem response, and a Result becomes a response. Run it with:
//   dotnet run --project samples/AspNetCore.Samples
using Wyzely.Functional.AspNetCoreSamples;

await OrdersExample.Run(Console.Out);

return 0;
