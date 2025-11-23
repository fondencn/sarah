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
            speechService.StartPlaySound(request.FileName);
            return Ok();
        }

        [HttpPost("StopAudio")]
        public IActionResult StopPlaySound([FromBody] StopSoundRequest request)
        {
            speechService.StopPlaySound();
            return Ok();
        }
    }
}
