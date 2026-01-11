using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sarah.API.Interfaces;
using Sarah.API.BusinessObjects;

namespace Sarah.EventProcessing.WebApi.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class EventsController : ControllerBase
{
    private readonly IEventProcessingService _eventProcessingService;
    private readonly ILogger<EventsController> _logger;

    public EventsController(IEventProcessingService eventProcessingService, ILogger<EventsController> logger)
    {
        _eventProcessingService = eventProcessingService;
        _logger = logger;
    }

    [HttpPost("say")]
    public async Task<IActionResult> PublishSayEvent([FromBody] SayEventRequest request)
    {
        try
        {
            var sayEvent = new SayEvent(request.Text, request.Hostname ?? "", request.Volume);
            
            await _eventProcessingService.PublishSay(sayEvent);
            return Ok(new { message = "Say event published successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing say event");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("status")]
    [AllowAnonymous]
    public IActionResult GetStatus()
    {
        return Ok(new { status = "running", service = "EventProcessingService" });
    }
}

public record SayEventRequest(string Text, string? Hostname = null, SpeechVolume Volume = SpeechVolume.Normal);
