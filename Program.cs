// <Configuration>
using Microsoft.AspNetCore.Http.Extensions;
using Orleans.Runtime;
using Orleans.Configuration;
using Orleans;

var builder = WebApplication.CreateBuilder();

if (builder.Environment.IsDevelopment())
{
    builder.Host.UseOrleans(builder =>
    {
        builder.UseLocalhostClustering();
        builder.AddMemoryGrainStorage("urls");
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
        var resultBuilder = new UriBuilder($"{ request.Scheme }://{ request.Host.Value}")
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

// <GrainInterface>
public interface IUrlShortenerGrain : IGrainWithStringKey
{
    Task SetUrl(string fullUrl);
    Task<string> GetUrl();
}
// </GrainInterface>

// <Grain>
public class UrlShortenerGrain : Grain, IUrlShortenerGrain
{
    private readonly IPersistentState<UrlDetails> _state;

    public UrlShortenerGrain(
        [PersistentState(
                stateName: "url",
                storageName: "urls")]
                IPersistentState<UrlDetails> state)
    {
        _state = state;
    }

    public async Task SetUrl(string fullUrl)
    {
        _state.State = new UrlDetails() { ShortenedRouteSegment = this.GetPrimaryKeyString(), FullUrl = fullUrl };
        await _state.WriteStateAsync();
    }

    public Task<string> GetUrl()
    {
        return Task.FromResult(_state.State.FullUrl);
    }
}

[GenerateSerializer]
public record UrlDetails
{
    public string FullUrl { get; set; }
    public string ShortenedRouteSegment { get; set; }
}
// </grain>