using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sarah.API.Interfaces.Services;

namespace Sarah.Rules.WebApi.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class RulesController : ControllerBase
{
    private readonly IRuleService _ruleService;
    private readonly ILogger<RulesController> _logger;

    public RulesController(IRuleService ruleService, ILogger<RulesController> logger)
    {
        _ruleService = ruleService;
        _logger = logger;
    }

    [HttpGet("status")]
    [AllowAnonymous]
    public IActionResult GetStatus()
    {
        return Ok(new { status = "running", service = "RuleService" });
    }
}
