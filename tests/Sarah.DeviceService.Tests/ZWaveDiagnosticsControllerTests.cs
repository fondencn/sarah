using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Sarah.DeviceService.Controllers;
using Sarah.DeviceService.WebApi.DTOs;
using Sarah.DeviceService.WebApi.Services;
using Xunit;

namespace Sarah.DeviceService.Tests;

public class ZWaveDiagnosticsControllerTests
{
    [Fact]
    public async Task GetNodes_WithoutController_Returns503()
    {
        var result = await CreateController(new FakeDiagnostics { Nodes = null }).GetNodes(CancellationToken.None);

        Assert.Equal(503, Assert.IsType<ObjectResult>(result).StatusCode);
    }

    [Fact]
    public async Task GetNodes_ReturnsNodes()
    {
        var fake = new FakeDiagnostics { Nodes = [new ZWaveNodeDiagnosticsDto { NodeId = 2, IsKnown = false }] };

        var result = await CreateController(fake).GetNodes(CancellationToken.None);

        var nodes = Assert.IsAssignableFrom<IReadOnlyList<ZWaveNodeDiagnosticsDto>>(Assert.IsType<OkObjectResult>(result).Value);
        Assert.Single(nodes);
    }

    [Fact]
    public async Task StartInclusion_StartsInclusionMode()
    {
        var fake = new FakeDiagnostics();

        await CreateController(fake).StartInclusion(CancellationToken.None);

        Assert.Equal(ZWaveInclusionMode.Inclusion, fake.StartedMode);
    }

    [Fact]
    public async Task StartExclusion_WithoutController_Returns503()
    {
        var fake = new FakeDiagnostics { StartThrows = new ZWaveControllerUnavailableException() };

        var result = await CreateController(fake).StartExclusion(CancellationToken.None);

        Assert.Equal(503, Assert.IsType<ObjectResult>(result).StatusCode);
    }

    [Fact]
    public async Task AssociationGroups_ForUnknownNode_AreRejected()
    {
        var fake = new FakeDiagnostics { Known = false };
        var controller = CreateController(fake);

        Assert.IsType<BadRequestObjectResult>(await controller.GetAssociationGroups(9, CancellationToken.None));
        Assert.IsType<BadRequestObjectResult>(await controller.SetAssociationGroup(9, 1, new SetZWaveAssociationRequest { NodeIds = [1] }, CancellationToken.None));
        Assert.False(fake.AssociationSet);
    }

    [Fact]
    public async Task SetAssociationGroup_ForKnownNode_Persists()
    {
        var fake = new FakeDiagnostics { Known = true };

        var result = await CreateController(fake).SetAssociationGroup(10, 1, new SetZWaveAssociationRequest { NodeIds = [1] }, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        Assert.True(fake.AssociationSet);
    }

    [Fact]
    public async Task SetAssociationGroup_GroupZero_IsRejected()
    {
        var result = await CreateController(new FakeDiagnostics { Known = true })
            .SetAssociationGroup(10, 0, new SetZWaveAssociationRequest(), CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Parameters_ForUnknownNode_AreRejected()
    {
        var fake = new FakeDiagnostics { Known = false };
        var controller = CreateController(fake);

        Assert.IsType<BadRequestObjectResult>(await controller.GetParameters(9, CancellationToken.None));
        Assert.IsType<BadRequestObjectResult>(await controller.SetParameter(9, 1, new SetZWaveParameterRequest { Value = 1 }, CancellationToken.None));
        Assert.Null(fake.ParameterSet);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(256)]
    public async Task SetParameter_OutOfByteRange_IsRejected(long value)
    {
        var fake = new FakeDiagnostics();

        var result = await CreateController(fake).SetParameter(10, 1, new SetZWaveParameterRequest { Value = value }, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Null(fake.ParameterSet);
    }

    [Fact]
    public async Task SetParameter_ReturnsReadBackValue()
    {
        var fake = new FakeDiagnostics();

        var result = await CreateController(fake).SetParameter(10, 7, new SetZWaveParameterRequest { Value = 42 }, CancellationToken.None);

        var dto = Assert.IsType<ZWaveParameterDto>(Assert.IsType<OkObjectResult>(result).Value);
        Assert.Equal((7, 42L), (dto.Id, dto.Value));
        Assert.Equal((7, 42L), fake.ParameterSet);
    }

    [Fact]
    public async Task Parameters_UnreachableDevice_Returns504()
    {
        var fake = new FakeDiagnostics { ParametersThrow = new ZWaveDeviceUnreachableException("asleep", new Exception()) };

        var result = await CreateController(fake).GetParameters(10, CancellationToken.None);

        Assert.Equal(504, Assert.IsType<ObjectResult>(result).StatusCode);
    }

    [Fact]
    public async Task SetParameter_UnsupportedParameter_Returns400()
    {
        var fake = new FakeDiagnostics { ParametersThrow = new ZWaveParameterNotSupportedException(99) };

        var result = await CreateController(fake).SetParameter(10, 99, new SetZWaveParameterRequest { Value = 1 }, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    private static ZWaveDiagnosticsController CreateController(IZWaveDiagnosticsService service) =>
        new(service, NullLogger<ZWaveDiagnosticsController>.Instance);

    private sealed class FakeDiagnostics : IZWaveDiagnosticsService
    {
        public IReadOnlyList<ZWaveNodeDiagnosticsDto>? Nodes { get; set; } = [];
        public bool Known { get; set; } = true;
        public Exception? StartThrows { get; set; }
        public ZWaveInclusionMode? StartedMode { get; private set; }
        public bool AssociationSet { get; private set; }
        public Exception? ParametersThrow { get; set; }
        public (int Id, long Value)? ParameterSet { get; private set; }

        public Task<IReadOnlyList<ZWaveNodeDiagnosticsDto>?> GetNodesAsync(CancellationToken ct) => Task.FromResult(Nodes);
        public ZWaveModeStatusDto GetModeStatus() => new();

        public Task<ZWaveModeStatusDto> StartModeAsync(ZWaveInclusionMode mode, CancellationToken ct)
        {
            if (StartThrows != null) throw StartThrows;
            StartedMode = mode;
            return Task.FromResult(new ZWaveModeStatusDto { Mode = mode });
        }

        public Task<ZWaveModeStatusDto> StopModeAsync(CancellationToken ct) => Task.FromResult(new ZWaveModeStatusDto());
        public bool IsKnownNode(byte nodeId) => Known;

        public Task<ZWaveDeviceParametersDto> GetParametersAsync(byte nodeId, CancellationToken ct)
        {
            if (ParametersThrow != null) throw ParametersThrow;
            return Task.FromResult(new ZWaveDeviceParametersDto { HasProvider = true });
        }

        public Task<ZWaveParameterDto> SetParameterAsync(byte nodeId, byte paramId, long value, CancellationToken ct)
        {
            if (ParametersThrow != null) throw ParametersThrow;
            ParameterSet = (paramId, value);
            return Task.FromResult(new ZWaveParameterDto { Id = paramId, Value = value });
        }

        public Task<IReadOnlyList<ZWaveAssociationGroupDto>> GetAssociationGroupsAsync(byte nodeId, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<ZWaveAssociationGroupDto>>([]);

        public Task SetAssociationGroupAsync(byte nodeId, byte groupId, byte[] nodeIds, CancellationToken ct)
        {
            AssociationSet = true;
            return Task.CompletedTask;
        }
    }
}
