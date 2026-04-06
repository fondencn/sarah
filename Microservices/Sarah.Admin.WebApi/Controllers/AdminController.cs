using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sarah.Messaging.RabbitMQ;
using Sarah.Messaging.RabbitMQ.Messages;

namespace Sarah.Admin.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AdminController(RabbitMQClient rabbitMQClient, ILogger<AdminController> logger) : ControllerBase
{
    /// <summary>
    /// Broadcasts a text-to-speech message to all connected speakers via RabbitMQ.
    /// </summary>
    [HttpPost("say")]
    public async Task<IActionResult> Say([FromBody] SayRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
            return BadRequest("Text must not be empty.");

        var volume = request.Volume ?? SpeechVolume.Normal;
        var message = new SayMessage(request.Text, TargetSpeaker: "", Volume: volume);

        await rabbitMQClient.PublishAsync(message);

        var sanitizedText = request.Text.Replace("\r", string.Empty).Replace("\n", string.Empty);
        logger.LogInformation("Broadcasted say message: '{Text}' (volume: {Volume})", sanitizedText, volume);
        return Ok();
    }
}

public class SayRequest
{
    public string Text { get; set; } = string.Empty;
    public SpeechVolume? Volume { get; set; }
}
