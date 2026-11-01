using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sarah.DeviceService.WebApi.DTOs;
using Sarah.DeviceService.WebApi.Services;

namespace Sarah.DeviceService.Controllers
{
    [ApiController]
    [Route("api/devices/zwave/diagnostics")]
    [Authorize]
    public class ZWaveDiagnosticsController(IZWaveDiagnosticsService diagnostics, ILogger<ZWaveDiagnosticsController> logger) : ControllerBase
    {
        [HttpGet("nodes")]
        [ProducesResponseType<IReadOnlyList<ZWaveNodeDiagnosticsDto>>(StatusCodes.Status200OK)]
        public Task<IActionResult> GetNodes(CancellationToken ct) => Run(async () =>
        {
            var nodes = await diagnostics.GetNodesAsync(ct);
            return nodes == null ? Unavailable() : Ok(nodes);
        });

        [HttpGet("mode")]
        [ProducesResponseType<ZWaveModeStatusDto>(StatusCodes.Status200OK)]
        public IActionResult GetMode() => Ok(diagnostics.GetModeStatus());

        [HttpPost("inclusion/start")]
        [ProducesResponseType<ZWaveModeStatusDto>(StatusCodes.Status200OK)]
        public Task<IActionResult> StartInclusion(CancellationToken ct) =>
            Run(async () => Ok(await diagnostics.StartModeAsync(ZWaveInclusionMode.Inclusion, ct)));

        [HttpPost("exclusion/start")]
        [ProducesResponseType<ZWaveModeStatusDto>(StatusCodes.Status200OK)]
        public Task<IActionResult> StartExclusion(CancellationToken ct) =>
            Run(async () => Ok(await diagnostics.StartModeAsync(ZWaveInclusionMode.Exclusion, ct)));

        [HttpPost("inclusion/stop")]
        [HttpPost("exclusion/stop")]
        [ProducesResponseType<ZWaveModeStatusDto>(StatusCodes.Status200OK)]
        public Task<IActionResult> StopMode(CancellationToken ct) =>
            Run(async () => Ok(await diagnostics.StopModeAsync(ct)));

        [HttpGet("nodes/{nodeId}/association-groups")]
        [ProducesResponseType<IReadOnlyList<ZWaveAssociationGroupDto>>(StatusCodes.Status200OK)]
        public Task<IActionResult> GetAssociationGroups(byte nodeId, CancellationToken ct) => Run(async () =>
        {
            if (!diagnostics.IsKnownNode(nodeId))
            {
                return UnknownNode(nodeId);
            }
            return Ok(await diagnostics.GetAssociationGroupsAsync(nodeId, ct));
        });

        [HttpPut("nodes/{nodeId}/association-groups/{groupId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public Task<IActionResult> SetAssociationGroup(byte nodeId, byte groupId, [FromBody] SetZWaveAssociationRequest request, CancellationToken ct) => Run(async () =>
        {
            if (!diagnostics.IsKnownNode(nodeId))
            {
                return UnknownNode(nodeId);
            }
            if (groupId == 0)
            {
                return BadRequest(new { message = "Association group ids start at 1" });
            }
            if (request.NodeIds.Any(id => id is < 1 or > 232))
            {
                return BadRequest(new { message = "Node ids must be between 1 and 232" });
            }
            await diagnostics.SetAssociationGroupAsync(nodeId, groupId, request.NodeIds.Select(id => (byte)id).ToArray(), ct);
            return NoContent();
        });

        [HttpGet("nodes/{nodeId}/parameters")]
        [ProducesResponseType<ZWaveDeviceParametersDto>(StatusCodes.Status200OK)]
        public Task<IActionResult> GetParameters(byte nodeId, CancellationToken ct) => Run(async () =>
        {
            if (!diagnostics.IsKnownNode(nodeId))
            {
                return UnknownNode(nodeId);
            }
            return Ok(await diagnostics.GetParametersAsync(nodeId, ct));
        });

        [HttpPut("nodes/{nodeId}/parameters/{paramId}")]
        [ProducesResponseType<ZWaveParameterDto>(StatusCodes.Status200OK)]
        public Task<IActionResult> SetParameter(byte nodeId, byte paramId, [FromBody] SetZWaveParameterRequest request, CancellationToken ct) => Run(async () =>
        {
            if (!diagnostics.IsKnownNode(nodeId))
            {
                return UnknownNode(nodeId);
            }
            // The provider writes a single byte.
            if (request.Value is < 0 or > 255)
            {
                return BadRequest(new { message = "Value must be between 0 and 255" });
            }
            return Ok(await diagnostics.SetParameterAsync(nodeId, paramId, request.Value, ct));
        });

        private IActionResult Unavailable() => StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "No ZWave controller available" });

        private IActionResult UnknownNode(byte nodeId) => BadRequest(new { message = $"Node {nodeId} is not known to the NodeFactory" });

        private async Task<IActionResult> Run(Func<Task<IActionResult>> action)
        {
            try
            {
                return await action();
            }
            catch (ZWaveControllerUnavailableException)
            {
                return Unavailable();
            }
            catch (ZWaveParameterNotSupportedException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (ZWaveDeviceUnreachableException ex)
            {
                return StatusCode(StatusCodes.Status504GatewayTimeout, new { message = ex.Message });
            }
            catch (ZWaveNodeNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (OperationCanceledException)
            {
                return StatusCode(StatusCodes.Status499ClientClosedRequest);
            }
            catch (TimeoutException ex)
            {
                logger.LogWarning(ex, "ZWave diagnostics request timed out");
                return StatusCode(StatusCodes.Status504GatewayTimeout, new { message = "ZWave device did not respond in time" });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "ZWave diagnostics request failed");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }
    }
}
