using Microsoft.AspNetCore.Http.Extensions;
using Orleans;
using System.Net;
using Orleans.Configuration;
using Orleans.Hosting;

var builder = WebApplication.CreateBuilder();

builder.Host.UseOrleans((Action<ISiloBuilder>)(siloBuilder =>
{
    if (builder.Environment.IsDevelopment())
    {
        InitDevelopment(builder);
    }
    else
    {
        builder.Host.UseOrleans(builder =>
        {
            var envCnn = Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING");

            var connectionString = envCnn ?? throw new InvalidOperationException("Missing connection string");
            builder
                .UseAzureStorageClustering(options => options.ConfigureTableServiceClient(connectionString))
                .AddAzureTableGrainStorage("users", options => options.ConfigureTableServiceClient(connectionString))
                .Configure<ClusterOptions>(options =>
                    {
                        options.ClusterId = "identitykeeper";
                        options.ServiceId = "identity";
                    })
                .UseDashboard( )
                .ConfigureLogging(logging =>
                {
                    logging.AddConsole();
                    logging.SetMinimumLevel(LogLevel.Warning);
                });

        });
    }

}));
//builder.Services.DontHostGrainsHere();
var app = builder.Build();
app.Map("/d", d => d.UseOrleansDashboard());
app.Run();

static void InitDevelopment(WebApplicationBuilder builder)
{
    builder.Host.UseOrleans(builder =>
    {
        builder.UseDevelopmentClustering(primarySiloEndpoint: new IPEndPoint(IPAddress.Loopback, 11111))
        .ConfigureEndpoints(IPAddress.Loopback, 11112, 30001)
        .UseDashboard(op =>
        {
            op.HostSelf = false;
            op.Port = 8083;
        });

    }).ConfigureLogging(logging =>
    {
        logging.AddConsole();
        logging.SetMinimumLevel(LogLevel.Warning);
    });
}