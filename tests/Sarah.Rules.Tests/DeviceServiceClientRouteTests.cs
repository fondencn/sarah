using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Sarah.API.BusinessObjects.SpeakerRequests;
using Sarah.ServiceClients;

namespace Sarah.Rules.Tests;

public class DeviceServiceClientRouteTests
{
    [Fact]
    public async Task GetOpenDoors_UsesModernDevicesEndpoint()
    {
        HttpRequestMessage? capturedRequest = null;

        var client = CreateClient(request =>
        {
            capturedRequest = request;
            return JsonResponse(new GetOpenDoorsResponse { OpenDoorInfo = "Front Door" });
        });

        var response = await client.GetOpenDoors();

        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Get, capturedRequest!.Method);
        Assert.Equal("/api/devices/open-doors", capturedRequest.RequestUri?.AbsolutePath);
        Assert.Equal("Front Door", response.OpenDoorInfo);
    }

    [Fact]
    public async Task ActivateScene_UsesSceneStartEndpointWithSceneTypeNamePayload()
    {
        HttpRequestMessage? capturedRequest = null;

        var client = CreateClient(request =>
        {
            capturedRequest = request;
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        await client.ActivateScene("RedAlert");

        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Post, capturedRequest!.Method);
        Assert.Equal("/api/devices/scenes/start", capturedRequest.RequestUri?.AbsolutePath);

        string body = await capturedRequest.Content!.ReadAsStringAsync();
        using JsonDocument document = JsonDocument.Parse(body);
        Assert.Equal("RedAlert", document.RootElement.GetProperty("SceneTypeName").GetString());
    }

    [Fact]
    public async Task DeactivateScene_UsesSceneStopEndpointWithSceneTypeNamePayload()
    {
        HttpRequestMessage? capturedRequest = null;

        var client = CreateClient(request =>
        {
            capturedRequest = request;
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        await client.DeactivateScene("RedAlert");

        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Post, capturedRequest!.Method);
        Assert.Equal("/api/devices/scenes/stop", capturedRequest.RequestUri?.AbsolutePath);

        string body = await capturedRequest.Content!.ReadAsStringAsync();
        using JsonDocument document = JsonDocument.Parse(body);
        Assert.Equal("RedAlert", document.RootElement.GetProperty("SceneTypeName").GetString());
    }

    private static DeviceServiceClient CreateClient(Func<HttpRequestMessage, HttpResponseMessage> handler)
    {
        var httpClient = new HttpClient(new StubMessageHandler(handler))
        {
            BaseAddress = new Uri("http://devices.test")
        };

        return new DeviceServiceClient(httpClient, NullLogger<DeviceServiceClient>.Instance);
    }

    private static HttpResponseMessage JsonResponse<T>(T payload)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };
    }

    private sealed class StubMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

        public StubMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_handler(request));
        }
    }
}
