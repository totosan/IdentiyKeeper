using Microsoft.AspNetCore.Http.Extensions;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;

var builder = WebApplication.CreateBuilder();

builder.Host.UseOrleans(siloBuilder =>
{
if (builder.Environment.IsDevelopment())
{
    builder.Host.UseOrleans(builder =>
    {
        builder.UseDevelopmentClustering(primarySiloEndpoint: new IPEndPoint(IPAddress.Loopback, 11112))
        .ConfigureEndpoints(IPAddress.Loopback, 11112, 30001)
        .UseDashboard(op =>
        {
            op.HostSelf = true;
            op.Port = 8080;
        });

    }).ConfigureLogging(logging =>
    {
        logging.AddConsole();
        logging.SetMinimumLevel(LogLevel.Warning);
    });
}
else
{
    builder.Host.UseOrleans(builder =>
    {
        var envCnn = Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING");

        var connectionString = envCnn ?? throw new InvalidOperationException("Missing connection string");
        builder.UseAzureStorageClustering(options =>
            options.ConfigureTableServiceClient(connectionString))
            .UseDashboard(op =>
            {
                op.Host = "*";
                op.HostSelf = true;
                op.Port = 8080;
            });

        builder.Configure<ClusterOptions>(options =>
        {
            options.ClusterId = "url-shortener";
            options.ServiceId = "urls";
        });
        
    });
}
  
});

var app = builder.Build();
app.Map("/dashboard", x => x.UseOrleansDashboard());
app.Run();