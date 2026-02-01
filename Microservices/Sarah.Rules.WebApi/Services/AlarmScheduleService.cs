using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sarah.API.BusinessObjects;
using Sarah.Rules.WebApi.Data;
using Sarah.Rules.Data.Entities;
using Sarah.Messaging.RabbitMQ;
using Sarah.Messaging.RabbitMQ.Messages;

namespace Sarah.Rules.Services;

/// <summary>
/// Manages alarm schedules and publishes change notifications.
/// Schedule changes trigger RuleService to recalculate timer configuration.
/// </summary>
public class AlarmScheduleService : IDisposable
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<AlarmScheduleService> _logger;
    private readonly RabbitMQClient _rabbitMQ;
    private CancellationTokenSource? _updateCancellationTokenSource;
    private Task? _updateTask;

    public AlarmScheduleService(ApplicationDbContext db, ILogger<AlarmScheduleService> logger, RabbitMQClient rabbitMQ)
    {
        _db = db;
        _logger = logger;
        _rabbitMQ = rabbitMQ;
    }

    /// <summary>
    /// Starts monitoring and scheduling of alarm schedules
    /// </summary>
    public Task Start()
    {
        _logger.LogDebug("AlarmScheduleService gestartet.");

        _updateCancellationTokenSource = new CancellationTokenSource();
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
            var oldAlarms = await _db.AlarmSchedules
                .Where(item => item.AlarmTime <= DateTime.Now && !item.HasRecurrence && item.IsActive)
                .ToListAsync();

            foreach (var alarm in oldAlarms)
            {
                alarm.IsActive = false;
                _db.AlarmSchedules.Update(alarm);
            }

            if (oldAlarms.Any())
            {
                await _db.SaveChangesAsync();
                _logger.LogDebug("Cleaned up {Count} old alarms", oldAlarms.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Aufräumen alter Alarme");
        }
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
        _logger.LogInformation("Alarm erstellt: {AlarmText} um {AlarmTime}", alarm.Text, alarm.AlarmTime);
        
        // Notify subscribers of the change
        await _rabbitMQ.PublishAsync(new AlarmScheduleChangedMessage 
        { 
            AlarmScheduleId = alarm.Id, 
            Change = AlarmScheduleChangedMessage.ChangeType.Created 
        });
        
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
        
        return alarm;
    }

    public void Dispose()
    {
        _updateCancellationTokenSource?.Cancel();
        _updateCancellationTokenSource?.Dispose();
    }
}
