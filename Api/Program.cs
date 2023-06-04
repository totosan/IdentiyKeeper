// <Configuration>
using Microsoft.AspNetCore.Http.Extensions;
using Orleans.Runtime;
using Orleans.Configuration;
using System.Net;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    builder.Host.UseOrleansClient(builder =>
    {
        builder.UseStaticClustering(new IPEndPoint(IPAddress.Loopback, 30000));
    });
} else
{
    builder.Host.UseOrleansClient(client =>
    {
        var envCnn = Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING");
        
        var connectionString = envCnn ?? throw new InvalidOperationException("Missing connection string");
        client.UseAzureStorageClustering(options => options.ConfigureTableServiceClient(connectionString));
        client.Configure<ClusterOptions>(options =>
        {
            options.ClusterId = "url-shortener";
            options.ServiceId = "urls";
        });
    }).ConfigureLogging(logging =>
    {
        logging.AddConsole();
        logging.SetMinimumLevel(LogLevel.Warning);
    });
}

var app = builder.Build();
// </Configuration>

// <Endpoints>
app.MapGet("/", () => "Hello World!");

app.MapGet("/shorten/{redirect}",
    async (IGrainFactory grains, HttpRequest request, string redirect) =>
    {
        // Create a unique, short ID
        var shortenedRouteSegment = Guid.NewGuid().GetHashCode().ToString("X");

        // Create and persist a grain with the shortened ID and full URL
        var shortenerGrain = grains.GetGrain<IUrlShortenerGrain>(shortenedRouteSegment);
        await shortenerGrain.SetUrl(redirect);

        // Return the shortened URL for later use
        var resultBuilder = new UriBuilder($"{request.Scheme}://{request.Host.Value}")
        {
            Path = $"/go/{shortenedRouteSegment}"
        };
        return Results.Ok(resultBuilder.Uri);
    });

app.MapGet("/go/{shortenedRouteSegment}",
    async (IGrainFactory grains, string shortenedRouteSegment) =>
    {
        // Retrieve the grain using the shortened ID and redirect to the original URL        
        var shortenerGrain = grains.GetGrain<IUrlShortenerGrain>(shortenedRouteSegment);
        var url = await shortenerGrain.GetUrl();

        return Results.Redirect($"http://{url}");
    });

app.Run();
// </Endpoints>
