using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Sarah.API.BusinessObjects;
using Sarah.API.Interfaces;
using Sarah.DeviceService.Model;
using Sarah.DeviceService.WebApi.Extensions;
using Sarah.DeviceService.WebApi.Services;
using Sarah.Messaging.RabbitMQ;
using Xunit;

namespace Sarah.DeviceService.Tests;

public class NodeFactoryTests
{
    [Fact]
    public void CreateByNodeId_ControllerNode_ReturnsControllerElement()
    {
        var factory = CreateFactory();

        var result = factory.CreateByNodeId(1);

        Assert.IsType<ControllerElement>(result);
    }

    [Fact]
    public void CreateByNodeId_WallPlugNode_ReturnsWallPlug()
    {
        var factory = CreateFactory();

        var result = factory.CreateByNodeId(10);

        Assert.IsType<ZWaveWallPlug>(result);
    }

    private static NodeFactory CreateFactory()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var rabbitMqClient = new RabbitMQClient(NullLogger<RabbitMQClient>.Instance, configuration);
        var publisher = new NetworkElementPublisher(rabbitMqClient);

        return new NodeFactory(
            NullLogger<NodeFactory>.Instance,
            publisher,
            configuration);
    }

}