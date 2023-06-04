using System.Net;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    builder.Host.UseOrleans(builder =>
    {
        builder.UseDevelopmentClustering( primarySiloEndpoint: new IPEndPoint(IPAddress.Loopback, 11111))
        .ConfigureEndpoints(IPAddress.Loopback, 11111,30000);

        builder.AddMemoryGrainStorage("urls");
    }).ConfigureLogging(logging =>
    {
        logging.AddConsole();
        logging.SetMinimumLevel(LogLevel.Warning);
    });
} else
{
    builder.Host.UseOrleans(builder =>
    {
        var envCnn = Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING");
        
        var connectionString = envCnn ?? throw new InvalidOperationException("Missing connection string");
        builder.UseAzureStorageClustering(options =>
            options.ConfigureTableServiceClient(connectionString))
            .AddAzureTableGrainStorage("urls",
                            options => options.ConfigureTableServiceClient(connectionString));
        builder.Configure<ClusterOptions>(options =>
        {
            options.ClusterId = "url-shortener";
            options.ServiceId = "urls";
        });
    });
}

// uncomment this if you dont mind hosting grains in the dashboard
//builder.Services.DontHostGrainsHere();

var app = builder.Build();

app.MapGet("/", () => Results.Ok("Silo"));

app.Run();