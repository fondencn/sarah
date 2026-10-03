using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Sarah.API.BusinessObjects;
using Sarah.Rules.Services.Kernel;

namespace Sarah.Rules.Tests;

public class SmartHomeKernelServiceBuildEventPromptTests
{
    [Fact]
    public void BuildEventPrompt_WithoutDeviceName_DoesNotContainDeviceNameLine()
    {
        var service = CreateService();
        var evt = new ClickedEvent(5, 1);

        string prompt = service.BuildEventPrompt(evt);

        Assert.DoesNotContain("DeviceName:", prompt);
        Assert.Contains("SourceNodeId: 5", prompt);
    }

    [Fact]
    public void BuildEventPrompt_WithDeviceName_ContainsDeviceNameLine()
    {
        var service = CreateService();
        var evt = new ClickedEvent(5, 1);

        string prompt = service.BuildEventPrompt(evt, deviceName: "Wohnzimmer Schalter");

        Assert.Contains("DeviceName: Wohnzimmer Schalter", prompt);
        Assert.Contains("SourceNodeId: 5", prompt);
    }

    [Fact]
    public void BuildEventPrompt_DeviceNameAppearsAfterSourceNodeId()
    {
        var service = CreateService();
        var evt = new ClickedEvent(7, 2);

        string prompt = service.BuildEventPrompt(evt, deviceName: "Küche Sensor");

        int nodeIdIndex = prompt.IndexOf("SourceNodeId:", StringComparison.Ordinal);
        int deviceNameIndex = prompt.IndexOf("DeviceName:", StringComparison.Ordinal);

        Assert.True(nodeIdIndex >= 0 && deviceNameIndex >= 0, "Both lines must be present");
        Assert.True(deviceNameIndex > nodeIdIndex, "DeviceName line must appear after SourceNodeId line");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void BuildEventPrompt_NullOrWhitespaceDeviceName_DoesNotContainDeviceNameLine(string? deviceName)
    {
        var service = CreateService();
        var evt = new ClickedEvent(3, 0);

        string prompt = service.BuildEventPrompt(evt, deviceName);

        Assert.DoesNotContain("DeviceName:", prompt);
    }

    private static SmartHomeKernelService CreateService()
    {
        var options = Options.Create(new SemanticKernelOptions
        {
            ConversationRetentionHours = 24,
            MaxHistoryMessages = 50
        });

        return new SmartHomeKernelService(
            scopeFactory: null!,
            options: options,
            promptProvider: null!,
            logger: NullLogger<SmartHomeKernelService>.Instance);
    }
}
