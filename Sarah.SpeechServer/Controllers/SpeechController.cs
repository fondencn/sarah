using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Sarah.API.BusinessObjects.SpeakerEvents;
using Sarah.API.Interfaces;

namespace Sarah.SpeechServer.Controllers
{
    [Route("Speech")]
    [ApiController]
    public class SpeechController(ISpeechService speechService) : ControllerBase
    {
        [HttpPost("Say")]
        public IActionResult Say([FromBody] SayRequest request)
        {
            speechService.SayWithVolume(request.Text, request.Volume);
            return Ok();
        }

        [HttpPost("StartAudio")]
        public IActionResult StartPlaySound([FromBody] PlaySoundRequest request)
        {
            return Ok();
        }

        [HttpPost("StopAudio")]
        public IActionResult StopPlaySound([FromBody] StopSoundRequest request)
        {
            // TODO: Implement logic to handle StopSoundRequest
            return Ok();
        }
    }
}
