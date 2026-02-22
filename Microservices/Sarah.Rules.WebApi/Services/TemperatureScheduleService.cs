using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sarah.Rules.WebApi.Data;
using Sarah.Rules.Data.Entities;
using Sarah.Messaging.RabbitMQ;
using Sarah.Messaging.RabbitMQ.Messages;

namespace Sarah.Rules.Services;

/// <summary>
/// Manages temperature schedules and publishes change notifications.
/// Schedule changes trigger RuleService to recalculate timer configuration.
/// </summary>
public class TemperatureScheduleService : IDisposable
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<TemperatureScheduleService> _logger;
    private readonly RabbitMQClient _rabbitMQ;

    public TemperatureScheduleService(ApplicationDbContext db, ILogger<TemperatureScheduleService> logger, RabbitMQClient rabbitMQ)
    {
        _db = db;
        _logger = logger;
        _rabbitMQ = rabbitMQ;
    }

    /// <summary>
    /// Gets all temperature schedules
    /// </summary>
    public async Task<IEnumerable<TemperatureScheduleEntity>> GetAllSchedulesAsync()
    {
        return await _db.TemperatureSchedules.ToListAsync();
    }

    /// <summary>
    /// Gets temperature schedules for a specific room
    /// </summary>
    public async Task<IEnumerable<TemperatureScheduleEntity>> GetSchedulesForRoomAsync(long roomId)
    {
        return await _db.TemperatureSchedules
            .Where(ts => ts.Id_Room == roomId)
            .ToListAsync();
    }

    /// <summary>
    /// Gets a temperature schedule by ID
    /// </summary>
    public async Task<TemperatureScheduleEntity?> GetScheduleByIdAsync(long id)
    {
        return await _db.TemperatureSchedules.FindAsync(id);
    }

    /// <summary>
    /// Creates a new temperature schedule
    /// </summary>
    public async Task<TemperatureScheduleEntity> CreateScheduleAsync(TemperatureScheduleEntity schedule)
    {
        if (schedule == null)
            throw new ArgumentNullException(nameof(schedule));

        _db.TemperatureSchedules.Add(schedule);
        await _db.SaveChangesAsync();
        _logger.LogInformation("Temperaturplan erstellt: Raum {RoomId}, Temperatur {Temp}°C um {Time}", 
            schedule.Id_Room, schedule.TargetTemperature, schedule.StartTime);
        
        // Notify subscribers of the change
        await _rabbitMQ.PublishAsync(new TemperatureScheduleChangedMessage 
        { 
            TemperatureScheduleId = schedule.Id, 
            RoomId = schedule.Id_Room,
            Change = TemperatureScheduleChangedMessage.ChangeType.Created 
        });
        
        return schedule;
    }

    /// <summary>
    /// Updates an existing temperature schedule
    /// </summary>
    public async Task<TemperatureScheduleEntity> UpdateScheduleAsync(long id, TemperatureScheduleEntity schedule)
    {
        var existing = await _db.TemperatureSchedules.FindAsync(id);
        if (existing == null)
            throw new KeyNotFoundException($"Temperaturplan mit ID {id} nicht gefunden");

        existing.Id_Room = schedule.Id_Room;
        existing.StartTime = schedule.StartTime;
        existing.TargetTemperature = schedule.TargetTemperature;
        existing.DayOfWeek = schedule.DayOfWeek;

        _db.TemperatureSchedules.Update(existing);
        await _db.SaveChangesAsync();
        _logger.LogInformation("Temperaturplan aktualisiert: {ScheduleId}", id);
        
        // Notify subscribers of the change
        await _rabbitMQ.PublishAsync(new TemperatureScheduleChangedMessage 
        { 
            TemperatureScheduleId = id, 
            RoomId = existing.Id_Room,
            Change = TemperatureScheduleChangedMessage.ChangeType.Updated 
        });
        
        return existing;
    }

    /// <summary>
    /// Deletes a temperature schedule
    /// </summary>
    public async Task DeleteScheduleAsync(long id)
    {
        var schedule = await _db.TemperatureSchedules.FindAsync(id);
        if (schedule == null)
            throw new KeyNotFoundException($"Temperaturplan mit ID {id} nicht gefunden");

        var roomId = schedule.Id_Room;
        _db.TemperatureSchedules.Remove(schedule);
        await _db.SaveChangesAsync();
        _logger.LogInformation("Temperaturplan gelöscht: {ScheduleId}", id);
        
        // Notify subscribers of the change
        await _rabbitMQ.PublishAsync(new TemperatureScheduleChangedMessage 
        { 
            TemperatureScheduleId = id, 
            RoomId = roomId,
            Change = TemperatureScheduleChangedMessage.ChangeType.Deleted 
        });
    }

    /// <summary>
    /// Gets the next scheduled temperature change for a room
    /// </summary>
    public async Task<TemperatureScheduleEntity?> GetNextScheduleForRoomAsync(long roomId)
    {
        var now = DateTime.Now;
        return await _db.TemperatureSchedules
            .Where(ts => ts.Id_Room == roomId && ts.StartTime > now)
            .OrderBy(ts => ts.StartTime)
            .FirstOrDefaultAsync();
    }

    public void Dispose()
    {
        // Cleanup if needed
    }
}
