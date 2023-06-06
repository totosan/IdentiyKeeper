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
    Console.WriteLine("configured host builder for development local");
}
else
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
    Console.WriteLine("configured host builder with Azure storage");
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

// ---- user identity ----

app.MapPost("/user",
    async (IGrainFactory grains, UserIdentity userIdentity) =>
    {
        // Create a grain for the user identity
        var userGrain = grains.GetGrain<IUserIdentityGrain>(userIdentity.Name);

        if (userGrain.GetActionName().Result == "Created")
        {
            return Results.BadRequest("User already exists");
        }
        // Set the user's name, email, and action name
        await userGrain.SetEmail(userIdentity.Email ?? "");

        // Return the user's name, email, and action name
        return Results.Ok(new
        {
            Name = await userGrain.GetName(),
            Email = await userGrain.GetEmail(),
            ActionName = await userGrain.GetActionName()
        });
    });

app.MapPut("/user",
async (IGrainFactory grains, UserIdentity userIdentity) =>
{
    // Create a grain for the user identity
    var userGrain = grains.GetGrain<IUserIdentityGrain>(userIdentity.Name);

    // Set the user's name, email, and action name
    await userGrain.SetName(userIdentity.Name);
    await userGrain.SetEmail(userIdentity.Email);
    await userGrain.SetActionName("Updated");

    // Return the user's name, email, and action name
    return Results.Ok(new
    {
        Name = await userGrain.GetName(),
        Email = await userGrain.GetEmail(),
        ActionName = await userGrain.GetActionName()
    });
});

app.MapGet("/user/{name}",
    async (IGrainFactory grains, string name) =>
    {
        // Retrieve the user's grain
        var userGrain = grains.GetGrain<IUserIdentityGrain>(name);

        // Return the user's name, email, and action name
        return Results.Ok(new
        {
            Name = await userGrain.GetName(),
            Email = await userGrain.GetEmail(),
            ActionName = await userGrain.GetActionName()
        });
    });


//create a MapGet endpoint for a loadtest of creating 1000 users
app.MapGet("/loadtest",
    async (IGrainFactory grains) =>
    {
        for (int i = 0; i < 1000; i++)
        {
            // Create a grain for the user identity
            var userGrain = grains.GetGrain<IUserIdentityGrain>($"loadtest{i}");
            await userGrain.SetEmail($"loadtest{i}@mail.de");
        }

        return Results.Ok();
    });
app.Run();
// </Endpoints>
