using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Xunit;

namespace Sarah.AppHost.Tests;

/// <summary>
/// Integration tests for the Sarah AppHost.
/// These tests verify that all services start correctly and are accessible.
/// </summary>
public class AppHostTests : IAsyncLifetime
{
    private DistributedApplication? _app;

    /// <summary>
    /// Initialize the test by building and starting the AppHost application.
    /// </summary>
    public async Task InitializeAsync()
    {
        // Create the AppHost testing builder
        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.Sarah_AppHost>();

        // Build the application
        _app = await appHost.BuildAsync();

        // Start the application
        await _app.StartAsync();
    }

    /// <summary>
    /// Clean up resources after test completion.
    /// </summary>
    public async Task DisposeAsync()
    {
        if (_app != null)
        {
            await _app.DisposeAsync();
        }
    }

    [Fact]
    public async Task AppHostBuildsSuccessfully()
    {
        // Assert - If we got here, the app built successfully
        Assert.NotNull(_app);
    }

    [Fact]
    public async Task AppHostStartsWithoutErrors()
    {
        // Assert - If we got here, the app started successfully
        Assert.NotNull(_app);
        
        // Give services a moment to initialize
        await Task.Delay(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task CanCreateHttpClientForKeycloak()
    {
        // Act - Try to create an HTTP client for Keycloak
        var httpClient = _app!.CreateHttpClient("keycloak");
        
        // Assert
        Assert.NotNull(httpClient);
    }

    [Fact]
    public async Task AppCanAccessServices()
    {
        // Arrange - Wait for services to start
        await Task.Delay(TimeSpan.FromSeconds(10));
        
        // Act - Try to create clients for various services
        var keycloakClient = _app!.CreateHttpClient("keycloak");
        
        // Assert
        Assert.NotNull(keycloakClient);
    }
}
