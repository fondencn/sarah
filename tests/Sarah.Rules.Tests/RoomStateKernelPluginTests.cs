using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Sarah.API.BusinessObjects;
using Sarah.API.BusinessObjects.DTOs;
using Sarah.Rules.Services.Kernel;
using Sarah.ServiceClients;

namespace Sarah.Rules.Tests;

public class RoomStateKernelPluginTests
{
    [Fact]
    public async Task GetAllRoomsAsync_ReturnsSortedRoomList()
    {
        var roomClient = CreateRoomClient(request =>
        {
            if (request.Method == HttpMethod.Get && request.RequestUri?.AbsolutePath == "/api/Rooms")
            {
                var rooms = new List<RoomDto>
                {
                    new() { Id = 2, Name = "Office" },
                    new() { Id = 1, Name = "Kitchen" }
                };

                return JsonResponse(rooms);
            }

            return NotFound();
        });

        var deviceClient = CreateDeviceClient(_ => NotFound());
        var plugin = new RoomStateKernelPlugin(roomClient, deviceClient);

        string result = await plugin.GetAllRoomsAsync();

        Assert.Contains("Raeume (2)", result);
        Assert.Contains("1: Kitchen", result);
        Assert.Contains("2: Office", result);
    }

    [Fact]
    public async Task GetDevicesInRoomAsync_ReturnsOnlyMatchingDevices()
    {
        var roomClient = CreateRoomClient(request =>
        {
            if (request.Method == HttpMethod.Get && request.RequestUri?.AbsolutePath == "/api/Rooms")
            {
                return JsonResponse(new[]
                {
                    new RoomDto { Id = 1, Name = "Kitchen" }
                });
            }

            return NotFound();
        });

        var deviceClient = CreateDeviceClient(request =>
        {
            if (request.Method == HttpMethod.Get && request.RequestUri?.AbsolutePath == "/api/Devices")
            {
                return JsonResponse(new[]
                {
                    new DeviceDto { Id = 10, RoomId = 1, Name = "Window Sensor", NodeId = 7, DeviceType = KnownDeviceTypes.AeotecDoorSensor, TypeName = "DoorSensor" },
                    new DeviceDto { Id = 11, RoomId = 2, Name = "Outside Device", NodeId = 8, DeviceType = KnownDeviceTypes.Unknown, TypeName = "Other" }
                });
            }

            return NotFound();
        });

        var plugin = new RoomStateKernelPlugin(roomClient, deviceClient);

        string result = await plugin.GetDevicesInRoomAsync(1);

        Assert.Contains("Geraete im Raum Kitchen", result);
        Assert.Contains("Window Sensor", result);
        Assert.DoesNotContain("Outside Device", result);
    }

    [Fact]
    public async Task GetRoomStateAsync_ContainsSummaryAndOpenDoorDeviceNames()
    {
        var roomClient = CreateRoomClient(request =>
        {
            if (request.Method == HttpMethod.Get && request.RequestUri?.AbsolutePath == "/api/Rooms")
            {
                return JsonResponse(new[]
                {
                    new RoomDto { Id = 1, Name = "Kitchen" }
                });
            }

            return NotFound();
        });

        var deviceClient = CreateDeviceClient(request =>
        {
            string path = request.RequestUri?.AbsolutePath ?? string.Empty;
            if (request.Method == HttpMethod.Get && path == "/api/devices/room/1/summary")
            {
                return JsonResponse(new RoomSummaryDto
                {
                    RoomId = 1,
                    AverageTemperature = 21.4,
                    AnyDoorOpen = true,
                    AnyPresence = true
                });
            }

            if (request.Method == HttpMethod.Get && path == "/api/Devices")
            {
                return JsonResponse(new[]
                {
                    new DeviceDto
                    {
                        Id = 10,
                        RoomId = 1,
                        Name = "Window Sensor",
                        NodeId = 7,
                        DeviceType = KnownDeviceTypes.AeotecDoorSensor,
                        TypeName = "DoorSensor",
                        DoorSensor = new DoorSensorStateDto { State = DoorSensorState.Offen }
                    },
                    new DeviceDto
                    {
                        Id = 11,
                        RoomId = 1,
                        Name = "Thermostat",
                        NodeId = 9,
                        DeviceType = KnownDeviceTypes.AeotecThermostat,
                        TypeName = "Thermostat"
                    }
                });
            }

            return NotFound();
        });

        var plugin = new RoomStateKernelPlugin(roomClient, deviceClient);

        string result = await plugin.GetRoomStateAsync(1);

        Assert.Contains("Raumzustand Kitchen", result);
        Assert.Contains("Temperatur 21", result);
        Assert.Contains("Praesenz ja", result);
        Assert.Contains("Window Sensor", result);
    }

    private static RoomServiceClient CreateRoomClient(Func<HttpRequestMessage, HttpResponseMessage> handler)
    {
        var httpClient = new HttpClient(new StubMessageHandler(handler))
        {
            BaseAddress = new Uri("http://rooms.test")
        };

        return new RoomServiceClient(httpClient, NullLogger<RoomServiceClient>.Instance);
    }

    private static DeviceServiceClient CreateDeviceClient(Func<HttpRequestMessage, HttpResponseMessage> handler)
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

    private static HttpResponseMessage NotFound() => new(HttpStatusCode.NotFound);

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
