using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sarah.API.BusinessObjects;
using Sarah.Messaging.RabbitMQ;
using Sarah.Messaging.RabbitMQ.Messages;
using Sarah.Rules.Data.Entities;
using Sarah.Rules.WebApi.Data;
using Sarah.Rules.WebApi.Options;

namespace Sarah.Rules.Services;

/// <summary>
/// Manages alarm schedules and publishes change notifications.
/// Schedule changes trigger RuleService to recalculate timer configuration.
/// </summary>
public class AlarmScheduleService : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();
    private readonly ApplicationDbContext _db;
    private readonly ILogger<AlarmScheduleService> _logger;
    private readonly RabbitMQClient _rabbitMQ;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly AlarmExecutionOptions _executionOptions;
    private readonly SemaphoreSlim _reloadLock = new(1, 1);
    private readonly ConcurrentDictionary<long, ScheduledAlarm> _scheduledAlarms = new();
    private CancellationTokenSource? _updateCancellationTokenSource;
    private Task? _updateTask;

    public AlarmScheduleService(
        ApplicationDbContext db,
        ILogger<AlarmScheduleService> logger,
        RabbitMQClient rabbitMQ,
        IServiceScopeFactory serviceScopeFactory,
        IOptions<AlarmExecutionOptions> executionOptions)
    {
        _db = db;
        _logger = logger;
        _rabbitMQ = rabbitMQ;
        _serviceScopeFactory = serviceScopeFactory;
        _executionOptions = executionOptions.Value;
    }

    /// <summary>
    /// Starts monitoring and scheduling of alarm schedules
    /// </summary>
    public Task Start()
    {
        _logger.LogDebug("AlarmScheduleService gestartet.");

        _updateCancellationTokenSource = new CancellationTokenSource();
        _ = RefreshSchedulesAsync();
        _updateTask = Task.Run(async () =>
        {
            // Initial delay, then clean up old alarms
            await Task.Delay(10 * 1000);
            await CleanupOldAlarms();

            // Every 12 hours check for alarms needing cleanup
            while (!_updateCancellationTokenSource.Token.IsCancellationRequested)
            {
                await Task.Delay(12 * 60 * 1000);
                await CleanupOldAlarms();
            }
        }, _updateCancellationTokenSource.Token);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Removes old one-time alarms that have already occurred
    /// Recurring alarms remain active
    /// </summary>
    private async Task CleanupOldAlarms()
    {
        try
        {
            // Use a separate scope to get a new DbContext instance for this background task
            // This prevents issues with disposed contexts when the main service is disposed
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var oldAlarms = await db.AlarmSchedules
                    .Where(item => item.AlarmTime <= DateTime.Now && !item.HasRecurrence && item.IsActive)
                    .ToListAsync();

                foreach (var alarm in oldAlarms)
                {
                    alarm.IsActive = false;
                    db.AlarmSchedules.Update(alarm);
                }

                if (oldAlarms.Any())
                {
                    await db.SaveChangesAsync();
                    _logger.LogDebug("Cleaned up {Count} old alarms", oldAlarms.Count);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Aufräumen alter Alarme");
        }
    }

    private async Task RefreshSchedulesAsync()
    {
        await _reloadLock.WaitAsync();
        try
        {
            foreach (var scheduled in _scheduledAlarms.Values)
            {
                scheduled.Dispose();
            }

            _scheduledAlarms.Clear();

            var alarms = await _db.AlarmSchedules
                .Where(a => a.IsActive)
                .OrderBy(a => a.AlarmTime)
                .ToListAsync();

            foreach (var alarm in alarms)
            {
                ScheduleAlarm(alarm);
            }

            _logger.LogDebug("Loaded {Count} active alarms into the scheduler", alarms.Count);
        }
        finally
        {
            _reloadLock.Release();
        }
    }

    private void ScheduleAlarm(AlarmScheduleEntity alarm)
    {
        var nextTriggerLocal = GetNextTriggerLocal(alarm, DateTime.Now);
        if (nextTriggerLocal == null)
        {
            _logger.LogDebug("Alarm {AlarmId} has no next trigger and will not be scheduled", alarm.Id);
            return;
        }

        var dueTime = nextTriggerLocal.Value - DateTime.Now;
        if (dueTime <= TimeSpan.Zero)
        {
            if (!alarm.HasRecurrence)
            {
                _logger.LogDebug("Alarm {AlarmId} is already due and will be handled by cleanup", alarm.Id);
                return;
            }

            dueTime = TimeSpan.FromSeconds(1);
        }

        var timer = new Timer(async _ => await HandleAlarmFiredAsync(alarm.Id), null, dueTime, Timeout.InfiniteTimeSpan);
        _scheduledAlarms[alarm.Id] = new ScheduledAlarm(timer, nextTriggerLocal.Value);

        _logger.LogDebug("Scheduled alarm {AlarmId} for {Trigger:O}", alarm.Id, nextTriggerLocal.Value);
    }

    private async Task HandleAlarmFiredAsync(long alarmId)
    {
        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var alarm = await db.AlarmSchedules.FindAsync(alarmId);
            if (alarm == null || !alarm.IsActive)
            {
                return;
            }

            bool suppressedBySummer = ShouldSuppressTemperatureAlarm(alarm);
            var triggerMessage = BuildTriggerMessage(alarm, suppressedBySummer);
            await _rabbitMQ.PublishAsync(triggerMessage);

            _logger.LogInformation(
                "Alarm {AlarmId} fired ({ContentType}), suppressedBySummer={Suppressed}",
                alarmId,
                alarm.ContentType,
                suppressedBySummer);

            if (!alarm.HasRecurrence)
            {
                alarm.IsActive = false;
                db.AlarmSchedules.Update(alarm);
                await db.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error firing alarm {AlarmId}", alarmId);
        }
        finally
        {
            await RefreshSchedulesAsync();
        }
    }

    private bool ShouldSuppressTemperatureAlarm(AlarmScheduleEntity alarm)
    {
        if (alarm.ContentType != AlarmContentType.TemperatureSchedule)
        {
            return false;
        }

        return _executionOptions.SuppressTemperatureAlarmsDuringSummer
               && _executionOptions.IsSummer(DateTime.Now);
    }

    private static AlarmTriggeredMessage BuildTriggerMessage(AlarmScheduleEntity alarm, bool suppressedBySummer)
    {
        return new AlarmTriggeredMessage
        {
            AlarmScheduleId = alarm.Id,
            ContentType = (int)alarm.ContentType,
            ContentJson = alarm.ContentJson,
            DisplayText = alarm.GetDisplayText(),
            TriggeredAtUtc = DateTime.Now,
            IsSuppressedBySummer = suppressedBySummer
        };
    }

    private DateTime? GetNextTriggerLocal(AlarmScheduleEntity alarm, DateTime localNow)
    {
        if (!alarm.HasRecurrence)
        {
            var alarmTimeLocal = NormalizeLocal(alarm.AlarmTime);
            return alarmTimeLocal > localNow ? alarmTimeLocal : null;
        }

        if (!string.IsNullOrWhiteSpace(alarm.SerializedRecurrence))
        {
            try
            {
                var recurrence = JsonSerializer.Deserialize<AlarmRecurrenceDefinition>(alarm.SerializedRecurrence, JsonOptions);
                if (recurrence != null)
                {
                    return recurrence.GetNextOccurrenceLocal(localNow);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse serialized recurrence for alarm {AlarmId}", alarm.Id);
            }
        }

        if (alarm.RecurrenceIsEnabled)
        {
            return CalculateFallbackRecurrence(alarm, localNow);
        }

        var fallback = NormalizeLocal(alarm.AlarmTime);
        return fallback > localNow ? fallback : null;
    }

    private static DateTime NormalizeLocal(DateTime value)
    {
        return value.Kind == DateTimeKind.Local
            ? value
            : DateTime.SpecifyKind(value, DateTimeKind.Local);
    }

    private DateTime? CalculateFallbackRecurrence(AlarmScheduleEntity alarm, DateTime localNow)
    {
        var baseTimeLocal = NormalizeLocal(alarm.AlarmTime);
        var targetTimeOfDay = baseTimeLocal.TimeOfDay;

        if (alarm.RecurrenceDayOfWeek < 0 || alarm.RecurrenceDayOfWeek > 6)
        {
            return null;
        }

        var targetDay = (DayOfWeek)alarm.RecurrenceDayOfWeek;
        var candidate = new DateTime(localNow.Year, localNow.Month, localNow.Day, targetTimeOfDay.Hours, targetTimeOfDay.Minutes, targetTimeOfDay.Seconds, DateTimeKind.Local);

        for (var i = 0; i < 14; i++)
        {
            if (candidate.DayOfWeek == targetDay && candidate > localNow)
            {
                return candidate;
            }

            candidate = candidate.AddDays(1);
        }

        return null;
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        return options;
    }

    /// <summary>
    /// Gets all active alarm schedules
    /// </summary>
    public async Task<IEnumerable<AlarmScheduleEntity>> GetActiveAlarmsAsync()
    {
        return await _db.AlarmSchedules
            .Where(a => a.IsActive)
            .ToListAsync();
    }

    /// <summary>
    /// Gets an alarm schedule by ID
    /// </summary>
    public async Task<AlarmScheduleEntity?> GetAlarmByIdAsync(long id)
    {
        return await _db.AlarmSchedules.FindAsync(id);
    }

    /// <summary>
    /// Gets all alarm schedules
    /// </summary>
    public async Task<IEnumerable<AlarmScheduleEntity>> GetAllAlarmsAsync()
    {
        return await _db.AlarmSchedules.ToListAsync();
    }

    /// <summary>
    /// Creates a new alarm schedule
    /// </summary>
    public async Task<AlarmScheduleEntity> CreateAlarmAsync(AlarmScheduleEntity alarm)
    {
        if (alarm == null)
            throw new ArgumentNullException(nameof(alarm));

        _db.AlarmSchedules.Add(alarm);
        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Alarm erstellt: {AlarmText} um {AlarmTime}",
            alarm.GetDisplayText(),
            alarm.AlarmTime);
        
        // Notify subscribers of the change
        await _rabbitMQ.PublishAsync(new AlarmScheduleChangedMessage 
        { 
            AlarmScheduleId = alarm.Id, 
            Change = AlarmScheduleChangedMessage.ChangeType.Created 
        });

        await RefreshSchedulesAsync();
        
        return alarm;
    }

    /// <summary>
    /// Updates an existing alarm schedule
    /// </summary>
    public async Task<AlarmScheduleEntity> UpdateAlarmAsync(long id, AlarmScheduleEntity alarm)
    {
        var existing = await _db.AlarmSchedules.FindAsync(id);
        if (existing == null)
            throw new KeyNotFoundException($"Alarm mit ID {id} nicht gefunden");

        existing.AlarmTime = alarm.AlarmTime;
        existing.Text = alarm.Text;
        existing.TargetSpeaker = alarm.TargetSpeaker;
        existing.IsActive = alarm.IsActive;
        existing.Volume = alarm.Volume;
        existing.ContentType = alarm.ContentType;
        existing.ContentJson = alarm.ContentJson;
        existing.IsRecurrence = alarm.IsRecurrence;
        existing.RecurrenceIsEnabled = alarm.RecurrenceIsEnabled;
        existing.RecurrenceDayOfWeek = alarm.RecurrenceDayOfWeek;
        existing.RecurrenceTime = alarm.RecurrenceTime;
        existing.IsNurInFerien = alarm.IsNurInFerien;
        existing.IsNichtInFerien = alarm.IsNichtInFerien;
        existing.HasRecurrence = alarm.HasRecurrence;
        existing.SerializedRecurrence = alarm.SerializedRecurrence;

        _db.AlarmSchedules.Update(existing);
        await _db.SaveChangesAsync();
        _logger.LogInformation("Alarm aktualisiert: {AlarmId}", id);
        
        // Notify subscribers of the change
        await _rabbitMQ.PublishAsync(new AlarmScheduleChangedMessage 
        { 
            AlarmScheduleId = id, 
            Change = AlarmScheduleChangedMessage.ChangeType.Updated 
        });

        await RefreshSchedulesAsync();
        
        return existing;
    }

    /// <summary>
    /// Deletes an alarm schedule
    /// </summary>
    public async Task DeleteAlarmAsync(long id)
    {
        var alarm = await _db.AlarmSchedules.FindAsync(id);
        if (alarm == null)
            throw new KeyNotFoundException($"Alarm mit ID {id} nicht gefunden");

        _db.AlarmSchedules.Remove(alarm);
        await _db.SaveChangesAsync();
        _logger.LogInformation("Alarm gelöscht: {AlarmId}", id);
        
        // Notify subscribers of the change
        await _rabbitMQ.PublishAsync(new AlarmScheduleChangedMessage 
        { 
            AlarmScheduleId = id, 
            Change = AlarmScheduleChangedMessage.ChangeType.Deleted 
        });

        await RefreshSchedulesAsync();
    }

    /// <summary>
    /// Toggles active state of an alarm
    /// </summary>
    public async Task<AlarmScheduleEntity> ToggleAlarmActiveAsync(long id)
    {
        var alarm = await _db.AlarmSchedules.FindAsync(id);
        if (alarm == null)
            throw new KeyNotFoundException($"Alarm mit ID {id} nicht gefunden");

        alarm.IsActive = !alarm.IsActive;
        _db.AlarmSchedules.Update(alarm);
        await _db.SaveChangesAsync();
        _logger.LogInformation("Alarm-Status gewechselt: {AlarmId}, IsActive: {IsActive}", id, alarm.IsActive);
        
        // Notify subscribers of the change
        await _rabbitMQ.PublishAsync(new AlarmScheduleChangedMessage 
        { 
            AlarmScheduleId = id, 
            Change = AlarmScheduleChangedMessage.ChangeType.ActivationStateChanged 
        });

        await RefreshSchedulesAsync();
        
        return alarm;
    }

    public void Dispose()
    {
        foreach (var scheduled in _scheduledAlarms.Values)
        {
            scheduled.Dispose();
        }

        _scheduledAlarms.Clear();

        _updateCancellationTokenSource?.Cancel();
        
        // Wait for the background task to complete before disposing
        if (_updateTask != null)
        {
            try
            {
                _updateTask.Wait(TimeSpan.FromSeconds(5));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error waiting for cleanup task to complete");
            }
        }
        
        _updateCancellationTokenSource?.Dispose();
        _reloadLock.Dispose();
    }

    private sealed class ScheduledAlarm : IDisposable
    {
        private readonly Timer _timer;

        public ScheduledAlarm(Timer timer, DateTime nextTriggerUtc)
        {
            _timer = timer;
            NextTriggerUtc = nextTriggerUtc;
        }

        public DateTime NextTriggerUtc { get; }

        public void Dispose()
        {
            _timer.Dispose();
        }
    }

    private sealed class AlarmRecurrenceDefinition
    {
        public string? Freq { get; set; }

        public int Interval { get; set; } = 1;

        public string[]? Byweekday { get; set; }

        public DateTime Dtstart { get; set; }

        public DateTime? Until { get; set; }

        public DateTime? GetNextOccurrenceLocal(DateTime localNow)
        {
            var startLocal = NormalizeLocal(Dtstart);
            var interval = Interval <= 0 ? 1 : Interval;
            var upperBound = Until.HasValue ? NormalizeLocal(Until.Value) : (DateTime?)null;

            if (startLocal > localNow && (upperBound == null || startLocal <= upperBound.Value))
            {
                return startLocal;
            }

            var frequency = (Freq ?? "daily").Trim().ToLowerInvariant();
            return frequency switch
            {
                "weekly" => GetNextWeeklyOccurrenceLocal(startLocal, localNow, interval, upperBound),
                "monthly" => GetNextMonthlyOccurrenceLocal(startLocal, localNow, interval, upperBound),
                "yearly" => GetNextYearlyOccurrenceLocal(startLocal, localNow, interval, upperBound),
                _ => GetNextDailyOccurrenceLocal(startLocal, localNow, interval, upperBound),
            };
        }

        private static DateTime? GetNextDailyOccurrenceLocal(DateTime startLocal, DateTime localNow, int interval, DateTime? upperBound)
        {
            var candidate = startLocal;
            while (candidate <= localNow)
            {
                candidate = candidate.AddDays(interval);
            }

            return upperBound.HasValue && candidate > upperBound.Value ? null : candidate;
        }

        private DateTime? GetNextWeeklyOccurrenceLocal(DateTime startLocal, DateTime localNow, int interval, DateTime? upperBound)
        {
            var weekdays = ParseWeekdays();
            if (weekdays.Count == 0)
            {
                weekdays.Add(startLocal.DayOfWeek);
            }

            var startDate = startLocal.Date;
            var timeOfDay = startLocal.TimeOfDay;

            for (var offset = 0; offset < 370; offset++)
            {
                var candidateDate = localNow.Date.AddDays(offset);
                var candidate = candidateDate.Add(timeOfDay);
                if (candidate <= localNow)
                {
                    continue;
                }

                if (!weekdays.Contains(candidate.DayOfWeek))
                {
                    continue;
                }

                var weeksSinceStart = (int)Math.Floor((candidateDate - startDate).TotalDays / 7d);
                if (weeksSinceStart < 0 || weeksSinceStart % interval != 0)
                {
                    continue;
                }

                if (upperBound.HasValue && candidate > upperBound.Value)
                {
                    return null;
                }

                return candidate;
            }

            return null;
        }

        private static DateTime? GetNextMonthlyOccurrenceLocal(DateTime startLocal, DateTime localNow, int interval, DateTime? upperBound)
        {
            var candidate = startLocal;
            while (candidate <= localNow)
            {
                candidate = candidate.AddMonths(interval);
            }

            return upperBound.HasValue && candidate > upperBound.Value ? null : candidate;
        }

        private static DateTime? GetNextYearlyOccurrenceLocal(DateTime startLocal, DateTime localNow, int interval, DateTime? upperBound)
        {
            var candidate = startLocal;
            while (candidate <= localNow)
            {
                candidate = candidate.AddYears(interval);
            }

            return upperBound.HasValue && candidate > upperBound.Value ? null : candidate;
        }

        private HashSet<DayOfWeek> ParseWeekdays()
        {
            var result = new HashSet<DayOfWeek>();
            if (Byweekday == null)
            {
                return result;
            }

            foreach (var weekday in Byweekday)
            {
                switch (weekday?.Trim().ToLowerInvariant())
                {
                    case "mo": result.Add(DayOfWeek.Monday); break;
                    case "tu": result.Add(DayOfWeek.Tuesday); break;
                    case "we": result.Add(DayOfWeek.Wednesday); break;
                    case "th": result.Add(DayOfWeek.Thursday); break;
                    case "fr": result.Add(DayOfWeek.Friday); break;
                    case "sa": result.Add(DayOfWeek.Saturday); break;
                    case "su": result.Add(DayOfWeek.Sunday); break;
                }
            }

            return result;
        }

        private static DateTime NormalizeLocal(DateTime value)
        {
            return value.Kind == DateTimeKind.Local ? value : DateTime.SpecifyKind(value, DateTimeKind.Local);
        }
    }
}
