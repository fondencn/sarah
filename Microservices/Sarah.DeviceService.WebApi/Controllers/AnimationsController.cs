using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sarah.API.Interfaces.Services;
using Sarah.DeviceService.DTOs;
using Sarah.DeviceService.Model.Animations;
using System.Linq;

namespace Sarah.DeviceService.Controllers
{
    [ApiController]
    [Route("api/devices/animations")]
    [Authorize]
    public class AnimationsController : ControllerBase
    {
        private readonly IDeviceService _deviceService;
        private readonly ILogger<AnimationsController> _logger;

        public AnimationsController(IDeviceService deviceService, ILogger<AnimationsController> logger)
        {
            _deviceService = deviceService;
            _logger = logger;
        }

        [HttpPost("blink")]
        public IActionResult ExecuteBlinkAnimation([FromBody] BlinkAnimationCommand command)
        {
            try
            {
                var lamp = _deviceService.Lamps.FirstOrDefault(l => l.NodeID == command.NodeId);
                if (lamp == null)
                {
                    _logger.LogWarning("Lamp with NodeId {NodeId} not found", command.NodeId);
                    return NotFound(new { message = $"Lamp with NodeId {command.NodeId} not found" });
                }

                var animation = new BlinkAnimation(lamp)
                {
                    BlinkCount = command.BlinkCount
                };
                animation.Start();

                _logger.LogInformation("Blink animation started for lamp {NodeId} with {BlinkCount} blinks", 
                    command.NodeId, command.BlinkCount);
                
                return Ok(new { message = "Blink animation started successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing blink animation for NodeId {NodeId}", command.NodeId);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpPost("/api/devices/scenes/start")]
        public IActionResult StartScene([FromBody] StartSceneCommand command)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(command.SceneTypeName))
                {
                    return BadRequest(new { message = "Scene type name is required" });
                }

                Scene.Start(command.SceneTypeName);

                _logger.LogInformation("Scene {SceneName} started successfully", command.SceneTypeName);
                
                return Ok(new { message = $"Scene {command.SceneTypeName} started successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting scene {SceneName}", command.SceneTypeName);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpPost("/api/devices/scenes/stop")]
        public IActionResult StopScene([FromBody] StopSceneCommand command)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(command.SceneTypeName))
                {
                    return BadRequest(new { message = "Scene type name is required" });
                }

                Scene.Stop(command.SceneTypeName);

                _logger.LogInformation("Scene {SceneName} stopped successfully", command.SceneTypeName);
                
                return Ok(new { message = $"Scene {command.SceneTypeName} stopped successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping scene {SceneName}", command.SceneTypeName);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }
    }
}
