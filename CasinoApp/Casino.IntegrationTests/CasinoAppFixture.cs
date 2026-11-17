namespace Casino.IntegrationTests;

public sealed class CasinoAppFixture : IAsyncLifetime
{
    public DistributedApplication App { get; private set; } = null!;
    public HttpClient Gateway { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.Casino_AppHost>();
        App = await builder.BuildAsync();

        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        await App.StartAsync(cts.Token);

        // Wait until the gateway reports it is running and healthy
        var notifications = App.Services.GetRequiredService<ResourceNotificationService>();
        await notifications.WaitForResourceAsync("gateway", KnownResourceStates.Running, cts.Token);

        Gateway = App.CreateHttpClient("gateway");
        Gateway.Timeout = TimeSpan.FromSeconds(30);
    }

    public async Task DisposeAsync()
    {
        Gateway.Dispose();
        await App.DisposeAsync();
    }
}

[CollectionDefinition(nameof(CasinoCollection))]
public sealed class CasinoCollection : ICollectionFixture<CasinoAppFixture> { }
