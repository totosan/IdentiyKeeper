using System.Net;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;


var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    builder.Host.UseOrleans(builder =>
    {
        builder.UseDevelopmentClustering(primarySiloEndpoint: new IPEndPoint(IPAddress.Loopback, 11111))
        .ConfigureEndpoints(IPAddress.Loopback, 11111, 30000)
        .UseDashboard(op =>
        {
            op.HostSelf = true;
            op.Port = 8083;
        });
        
        builder.AddMemoryGrainStorage("users");

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
            .AddAzureTableGrainStorage("users", options => options.ConfigureTableServiceClient(connectionString))
            .UseDashboard(op => {
                op.HostSelf = false;
                op.Port = 8083;
            });
        builder.Configure<SiloOptions>(options =>
        {
            options.SiloName = "usersSilo";
        });

        builder.Configure<ClusterOptions>(options =>
        {
            options.ClusterId = "identitykeeper";
            options.ServiceId = "identity";
        });
    });
}

var app = builder.Build();
app.Urls.Add("http://localhost:8080");
app.MapGet("/", () => Results.Ok("Silo"));


app.Run();