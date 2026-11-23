// ASP.NET CORE: from an Error to a problem response.
//
// Scenario: an endpoint looks up an order. The service returns a Result<Order>, and ToHttpResult turns it into the response: a value is a 200, and an
// error is a problem response (RFC 9457, application/problem+json) whose status comes from the kind of the error.
//
// WHY it is written this way:
//  - The library has no dependency on ASP.NET Core and no HTTP mapping on purpose (a mapping is a policy of the application), so the mapping is code
//    of your own. ProblemMapping.cs is the whole of it.
//  - The detail of the response is RedactedMessage, never Message: the error of this example carries an e-mail address as a sensitive detail, and
//    this example makes a real request to show that it does not reach the client.
//  - The app listens on a loopback port that the system chooses, and is stopped at the end, so the example can run anywhere.
using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;

namespace Wyzely.Functional.AspNetCoreSamples;

public static class OrdersExample
{
    private static readonly JsonSerializerOptions Indented = new() { WriteIndented = true };

    public static async Task Run(TextWriter output)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.ConfigureKestrel(static options => options.Listen(IPAddress.Loopback, 0));
        builder.Logging.ClearProviders();

        await using var app = builder.Build();
        app.MapOrders();
        await app.StartAsync();

        var server = app.Services.GetRequiredService<IServer>();
        var addresses = server.Features.Get<IServerAddressesFeature>();
        var address = addresses!.Addresses.Single();
        using var client = new HttpClient { BaseAddress = new Uri(address) };

        await Print(output, client, "/orders/1");
        await Print(output, client, "/orders/2");

        await app.StopAsync();
    }

    private static async Task Print(TextWriter output, HttpClient client, string path)
    {
        using var response = await client.GetAsync(path);
        var body = await response.Content.ReadAsStringAsync();
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        using var json = JsonDocument.Parse(body);
        var indented = JsonSerializer.Serialize(json.RootElement, Indented);

        await output.WriteLineAsync($"GET {path}: {(int)response.StatusCode} {mediaType}");
        await output.WriteLineAsync(indented);
    }
}
