using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sarah.API.BusinessObjects;
using Sarah.API.Interfaces.Services;
using Sarah.API.BusinessObjects.DTOs;
using Sarah.Rules.Services;
using Sarah.Rules.Services.Kernel;
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
    private readonly SmartHomePromptProvider _promptProvider;
    private readonly SmartHomePromptRuleStore _promptRuleStore;
    private readonly ILogger<RulesController> _logger;

    public RulesController(IRuleService ruleService, AlarmScheduleService alarmService, 
        TemperatureScheduleService temperatureService, ApplicationDbContext db,
        SmartHomePromptProvider promptProvider, SmartHomePromptRuleStore promptRuleStore,
        ILogger<RulesController> logger)
    {
        _ruleService = ruleService;
        _alarmService = alarmService;
        _temperatureService = temperatureService;
        _db = db;
        _promptProvider = promptProvider;
        _promptRuleStore = promptRuleStore;
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

    /// <summary>
    /// Gets kernel conversation history entries sorted newest to oldest.
    /// </summary>
    [HttpGet("kernel-conversation")]
    public async Task<ActionResult<IEnumerable<KernelConversationMessageDto>>> GetKernelConversationHistory(
        [FromQuery] string conversationId = "smart-home-main",
        [FromQuery] int hours = 2,
        [FromQuery] int limit = 200)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(conversationId))
                return BadRequest(new { message = "conversationId darf nicht leer sein" });

            if (hours < 1 || hours > 168)
                return BadRequest(new { message = "hours muss zwischen 1 und 168 liegen" });

            if (limit < 1 || limit > 1000)
                return BadRequest(new { message = "limit muss zwischen 1 und 1000 liegen" });

            DateTime cutoffUtc = DateTime.UtcNow.AddHours(-hours);

            var entries = await _db.KernelConversationMessages
                .Where(m => m.ConversationId == conversationId && m.CreatedAtUtc >= cutoffUtc)
                .OrderByDescending(m => m.CreatedAtUtc)
                .Take(limit)
                .Select(m => new KernelConversationMessageDto
                {
                    Id = m.Id,
                    ConversationId = m.ConversationId,
                    Role = m.Role,
                    Content = m.Content,
                    CreatedAtUtc = m.CreatedAtUtc
                })
                .ToListAsync();

            return Ok(entries);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting kernel conversation history");
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

    #region Prompt Rule CRUD

    /// <summary>
    /// Gets all prompt rules used by the semantic kernel system prompt.
    /// </summary>
    [HttpGet("prompt-rules")]
    public async Task<ActionResult<IEnumerable<PromptRuleDto>>> GetPromptRules()
    {
        try
        {
            var entities = await _db.PromptRules
                .OrderBy(r => r.SortOrder)
                .ThenBy(r => r.Name)
                .ToListAsync();

            var rules = entities.Select(ToPromptRuleDto).ToList();

            return Ok(rules);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting prompt rules");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Gets one prompt rule by id.
    /// </summary>
    [HttpGet("prompt-rules/{id:long}")]
    public async Task<ActionResult<PromptRuleDto>> GetPromptRuleById(long id)
    {
        try
        {
            var entity = await _db.PromptRules.FirstOrDefaultAsync(r => r.Id == id);
            if (entity == null)
            {
                return NotFound(new { message = $"Prompt-Regel mit ID {id} nicht gefunden" });
            }

            return Ok(ToPromptRuleDto(entity));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting prompt rule {PromptRuleId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Creates a prompt rule.
    /// </summary>
    [HttpPost("prompt-rules")]
    public async Task<ActionResult<PromptRuleDto>> CreatePromptRule([FromBody] PromptRuleUpsertDto dto)
    {
        try
        {
            if (!TryValidatePromptRule(dto, out var validationError))
            {
                return BadRequest(new { message = validationError });
            }

            var now = DateTime.UtcNow;
            var entity = new PromptRuleEntity
            {
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };

            ApplyPromptRuleDto(entity, dto);

            _db.PromptRules.Add(entity);
            await _db.SaveChangesAsync();

            await ReloadPromptRulesAsync();

            return CreatedAtAction(nameof(GetPromptRuleById), new { id = entity.Id }, ToPromptRuleDto(entity));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating prompt rule");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Updates a prompt rule.
    /// </summary>
    [HttpPut("prompt-rules/{id:long}")]
    public async Task<ActionResult<PromptRuleDto>> UpdatePromptRule(long id, [FromBody] PromptRuleUpsertDto dto)
    {
        try
        {
            if (!TryValidatePromptRule(dto, out var validationError))
            {
                return BadRequest(new { message = validationError });
            }

            var entity = await _db.PromptRules.FirstOrDefaultAsync(r => r.Id == id);
            if (entity == null)
            {
                return NotFound(new { message = $"Prompt-Regel mit ID {id} nicht gefunden" });
            }

            ApplyPromptRuleDto(entity, dto);
            entity.UpdatedAtUtc = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            await ReloadPromptRulesAsync();

            return Ok(ToPromptRuleDto(entity));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating prompt rule {PromptRuleId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Deletes a prompt rule.
    /// </summary>
    [HttpDelete("prompt-rules/{id:long}")]
    public async Task<IActionResult> DeletePromptRule(long id)
    {
        try
        {
            var entity = await _db.PromptRules.FirstOrDefaultAsync(r => r.Id == id);
            if (entity == null)
            {
                return NotFound(new { message = $"Prompt-Regel mit ID {id} nicht gefunden" });
            }

            _db.PromptRules.Remove(entity);
            await _db.SaveChangesAsync();

            await ReloadPromptRulesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting prompt rule {PromptRuleId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    private static PromptRuleDto ToPromptRuleDto(PromptRuleEntity entity)
    {
        PromptRuleTimerDto? timer = null;
        if (entity.TimerHour.HasValue && entity.TimerMinute.HasValue && entity.TimerWeekdays.HasValue)
        {
            timer = new PromptRuleTimerDto
            {
                Hour = entity.TimerHour.Value,
                Minute = entity.TimerMinute.Value,
                Weekdays = (long)entity.TimerWeekdays.Value,
                Interval = entity.TimerInterval.HasValue ? (int)entity.TimerInterval.Value : 0,
                FromUtc = entity.TimerFromUtc,
                UntilUtc = entity.TimerUntilUtc
            };
        }

        return new PromptRuleDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Guidance = entity.Guidance,
            IsEnabled = entity.IsEnabled,
            SortOrder = entity.SortOrder,
            Timer = timer
        };
    }

    private static void ApplyPromptRuleDto(PromptRuleEntity entity, PromptRuleUpsertDto dto)
    {
        entity.Name = dto.Name.Trim();
        entity.Guidance = dto.Guidance.Trim();
        entity.IsEnabled = dto.IsEnabled;
        entity.SortOrder = dto.SortOrder;

        if (dto.Timer == null)
        {
            entity.TimerHour = null;
            entity.TimerMinute = null;
            entity.TimerWeekdays = null;
            entity.TimerInterval = null;
            entity.TimerFromUtc = null;
            entity.TimerUntilUtc = null;
            return;
        }

        entity.TimerHour = dto.Timer.Hour;
        entity.TimerMinute = dto.Timer.Minute;
        entity.TimerWeekdays = (Weekdays)dto.Timer.Weekdays;
        entity.TimerInterval = (RecurrenceInterval)dto.Timer.Interval;
        entity.TimerFromUtc = dto.Timer.FromUtc;
        entity.TimerUntilUtc = dto.Timer.UntilUtc;
    }

    private static bool TryValidatePromptRule(PromptRuleUpsertDto? dto, out string? error)
    {
        if (dto == null)
        {
            error = "Prompt-Regel-Daten sind erforderlich";
            return false;
        }

        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            error = "Name ist erforderlich";
            return false;
        }

        if (string.IsNullOrWhiteSpace(dto.Guidance))
        {
            error = "Guidance ist erforderlich";
            return false;
        }

        if (dto.Timer != null)
        {
            if (dto.Timer.Hour < 0 || dto.Timer.Hour > 23)
            {
                error = "Timer-Stunde muss zwischen 0 und 23 liegen";
                return false;
            }

            if (dto.Timer.Minute < 0 || dto.Timer.Minute > 59)
            {
                error = "Timer-Minute muss zwischen 0 und 59 liegen";
                return false;
            }

            if (dto.Timer.Weekdays <= 0)
            {
                error = "Timer-Wochentage muessen gesetzt sein";
                return false;
            }

            if (!Enum.IsDefined(typeof(RecurrenceInterval), dto.Timer.Interval))
            {
                error = "Timer-Intervall ist ungueltig";
                return false;
            }
        }

        error = null;
        return true;
    }

    private async Task ReloadPromptRulesAsync(CancellationToken cancellationToken = default)
    {
        await _promptProvider.ReloadAsync(cancellationToken);
        _promptRuleStore.ReloadFromProvider();
    }

    #endregion
}
