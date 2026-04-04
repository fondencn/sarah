using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sarah.API.BusinessObjects;
using Sarah.API.Interfaces.Services;
using Sarah.API.BusinessObjects.DTOs;
using Sarah.Rules.Services;
using Sarah.Rules.Data.Entities;
using Sarah.Rules.WebApi.Data;
using Sarah.Rules.WebApi.Data.Entities;

namespace Sarah.Rules.WebApi.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class RulesController : ControllerBase
{
    private readonly IRuleService _ruleService;
    private readonly AlarmScheduleService _alarmService;
    private readonly TemperatureScheduleService _temperatureService;
    private readonly ApplicationDbContext _db;
    private readonly ILogger<RulesController> _logger;

    public RulesController(IRuleService ruleService, AlarmScheduleService alarmService, 
        TemperatureScheduleService temperatureService, ApplicationDbContext db, ILogger<RulesController> logger)
    {
        _ruleService = ruleService;
        _alarmService = alarmService;
        _temperatureService = temperatureService;
        _db = db;
        _logger = logger;
    }

    [HttpGet("status")]
    [AllowAnonymous]
    public IActionResult GetStatus()
    {
        try
        {
            var status = new RuleStatusDto
            {
                Status = "Running",
                ActiveRules = 0, // RuleService doesn't expose rule count in interface
                LastExecution = DateTime.UtcNow
            };
            
            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting rules status");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Gets all rules with their overview information (runtime rules from registered rule stores)
    /// </summary>
    [HttpGet]
    public ActionResult<IEnumerable<RuleOverviewDto>> GetRules()
    {
        try
        {
            var rules = _ruleService.Rules
                .OrderBy(r => r.Name)
                .Select(r => new RuleOverviewDto
                {
                    Name = r.Name,
                    LastOccurence = r.LastOccurence,
                    HasCondition = r.Condition != null,
                    HasAction = r.Action != null
                })
                .ToList();

            return Ok(rules);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting rules");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Gets the most recent rule execution log entries
    /// </summary>
    [HttpGet("log")]
    public async Task<ActionResult<IEnumerable<RuleExecutionLogDto>>> GetRuleExecutionLog([FromQuery] int limit = 100)
    {
        try
        {
            if (limit < 1 || limit > 1000)
                return BadRequest(new { message = "limit muss zwischen 1 und 1000 liegen" });

            var logs = await _db.RuleExecutionLogs
                .OrderByDescending(l => l.TriggeredAt)
                .Take(limit)
                .Select(l => new RuleExecutionLogDto
                {
                    Id = l.Id,
                    RuleName = l.RuleName,
                    TriggeredAt = l.TriggeredAt,
                    Success = l.Success,
                    ErrorMessage = l.ErrorMessage
                })
                .ToListAsync();

            return Ok(logs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting rule execution log");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    #region Alarm Schedule CRUD

    /// <summary>
    /// Gets all alarm schedules
    /// </summary>
    [HttpGet("alarms")]
    public async Task<ActionResult<IEnumerable<AlarmScheduleEntity>>> GetAllAlarms()
    {
        try
        {
            var alarms = await _alarmService.GetAllAlarmsAsync();
            return Ok(alarms);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all alarms");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Gets active alarm schedules
    /// </summary>
    [HttpGet("alarms/active")]
    public async Task<ActionResult<IEnumerable<AlarmScheduleEntity>>> GetActiveAlarms()
    {
        try
        {
            var alarms = await _alarmService.GetActiveAlarmsAsync();
            return Ok(alarms);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active alarms");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Gets a specific alarm schedule by ID
    /// </summary>
    [HttpGet("alarms/{id}")]
    public async Task<ActionResult<AlarmScheduleEntity>> GetAlarmById(long id)
    {
        try
        {
            var alarm = await _alarmService.GetAlarmByIdAsync(id);
            if (alarm == null)
                return NotFound(new { message = $"Alarm mit ID {id} nicht gefunden" });
            
            return Ok(alarm);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting alarm {AlarmId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Creates a new alarm schedule
    /// </summary>
    [HttpPost("alarms")]
    public async Task<ActionResult<AlarmScheduleEntity>> CreateAlarm([FromBody] AlarmScheduleEntity alarm)
    {
        try
        {
            if (alarm == null)
                return BadRequest(new { message = "Alarm data is required" });

            var created = await _alarmService.CreateAlarmAsync(alarm);
            return CreatedAtAction(nameof(GetAlarmById), new { id = created.Id }, created);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating alarm");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Updates an existing alarm schedule
    /// </summary>
    [HttpPut("alarms/{id}")]
    public async Task<ActionResult<AlarmScheduleEntity>> UpdateAlarm(long id, [FromBody] AlarmScheduleEntity alarm)
    {
        try
        {
            if (alarm == null)
                return BadRequest(new { message = "Alarm data is required" });

            var updated = await _alarmService.UpdateAlarmAsync(id, alarm);
            return Ok(updated);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Alarm not found {AlarmId}", id);
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating alarm {AlarmId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Deletes an alarm schedule
    /// </summary>
    [HttpDelete("alarms/{id}")]
    public async Task<IActionResult> DeleteAlarm(long id)
    {
        try
        {
            await _alarmService.DeleteAlarmAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Alarm not found {AlarmId}", id);
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting alarm {AlarmId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Toggles the active state of an alarm
    /// </summary>
    [HttpPatch("alarms/{id}/toggle")]
    public async Task<ActionResult<AlarmScheduleEntity>> ToggleAlarm(long id)
    {
        try
        {
            var alarm = await _alarmService.ToggleAlarmActiveAsync(id);
            return Ok(alarm);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Alarm not found {AlarmId}", id);
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling alarm {AlarmId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    #endregion

    #region Temperature Schedule CRUD

    /// <summary>
    /// Gets all temperature schedules
    /// </summary>
    [HttpGet("temperatures")]
    public async Task<ActionResult<IEnumerable<TemperatureScheduleEntity>>> GetAllTemperatureSchedules()
    {
        try
        {
            var schedules = await _temperatureService.GetAllSchedulesAsync();
            return Ok(schedules);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all temperature schedules");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Gets temperature schedules for a specific room
    /// </summary>
    [HttpGet("temperatures/room/{roomId}")]
    public async Task<ActionResult<IEnumerable<TemperatureScheduleEntity>>> GetRoomTemperatureSchedules(long roomId)
    {
        try
        {
            var schedules = await _temperatureService.GetSchedulesForRoomAsync(roomId);
            return Ok(schedules);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting temperature schedules for room {RoomId}", roomId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Gets a specific temperature schedule by ID
    /// </summary>
    [HttpGet("temperatures/{id}")]
    public async Task<ActionResult<TemperatureScheduleEntity>> GetTemperatureScheduleById(long id)
    {
        try
        {
            var schedule = await _temperatureService.GetScheduleByIdAsync(id);
            if (schedule == null)
                return NotFound(new { message = $"Temperaturplan mit ID {id} nicht gefunden" });
            
            return Ok(schedule);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting temperature schedule {ScheduleId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Creates a new temperature schedule
    /// </summary>
    [HttpPost("temperatures")]
    public async Task<ActionResult<TemperatureScheduleEntity>> CreateTemperatureSchedule([FromBody] TemperatureScheduleEntity schedule)
    {
        try
        {
            if (schedule == null)
                return BadRequest(new { message = "Temperature schedule data is required" });

            var created = await _temperatureService.CreateScheduleAsync(schedule);
            return CreatedAtAction(nameof(GetTemperatureScheduleById), new { id = created.Id }, created);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating temperature schedule");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Updates an existing temperature schedule
    /// </summary>
    [HttpPut("temperatures/{id}")]
    public async Task<ActionResult<TemperatureScheduleEntity>> UpdateTemperatureSchedule(long id, [FromBody] TemperatureScheduleEntity schedule)
    {
        try
        {
            if (schedule == null)
                return BadRequest(new { message = "Temperature schedule data is required" });

            var updated = await _temperatureService.UpdateScheduleAsync(id, schedule);
            return Ok(updated);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Temperature schedule not found {ScheduleId}", id);
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating temperature schedule {ScheduleId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Deletes a temperature schedule
    /// </summary>
    [HttpDelete("temperatures/{id}")]
    public async Task<IActionResult> DeleteTemperatureSchedule(long id)
    {
        try
        {
            await _temperatureService.DeleteScheduleAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Temperature schedule not found {ScheduleId}", id);
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting temperature schedule {ScheduleId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Gets the next scheduled temperature change for a room
    /// </summary>
    [HttpGet("temperatures/room/{roomId}/next")]
    public async Task<ActionResult<TemperatureScheduleEntity>> GetNextRoomTemperatureSchedule(long roomId)
    {
        try
        {
            var schedule = await _temperatureService.GetNextScheduleForRoomAsync(roomId);
            if (schedule == null)
                return NotFound(new { message = $"Kein zukünftiger Temperaturplan für Raum {roomId} gefunden" });
            
            return Ok(schedule);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting next temperature schedule for room {RoomId}", roomId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    #endregion
}
