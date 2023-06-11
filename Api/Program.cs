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
            options.ClusterId = "identitykeeper";
            options.ServiceId = "identity";
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
        await userGrain.SetName(userIdentity.Name);
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
app.MapGet("/users",
    async (IGrainFactory grains) =>
    {
        // Retrieve the user's grain
        var registry = grains.GetGrain<IUserRegistryGrain>(0);
        var users = await registry.GetUsers();
        // Return the user's name, email, and action name
        return Results.Ok(users);
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
        // Create 100 user identities
        Parallel.For(0, 100, async i =>
        {
            // Create a grain for the user identity
            var userGrain = grains.GetGrain<IUserIdentityGrain>($"user{i}");

            await userGrain.SetName($"user{i}");
            // Set the initial email address for the user
            await userGrain.SetEmail($"user{i}@mail.com");

            // Randomly change the email address for the user 1000 times
            for (int j = 0; j < 100; j++)
            {
                // Generate a random email address
                var randomEmail = $"{Guid.NewGuid().ToString().Substring(0, 8)}@mail.com";

                // Set the new email address for the user
                await userGrain.SetEmail(randomEmail);
            }
        });
        return Results.Ok();
    });

app.MapGet("/clear",
    async (IGrainFactory grains) =>
    {
        // get 100 user identities
        for (int i = 0; i < 100; i++)
        {
            // get a grain for the user identity
            var userGrain = grains.GetGrain<IUserIdentityGrain>($"user{i}");

            await userGrain.ClearState();
        }
        return Results.Ok();
    });


app.Run();
// </Endpoints>
