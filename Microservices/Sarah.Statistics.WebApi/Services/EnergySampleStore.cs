using Sarah.Messaging.RabbitMQ.Messages;
using Sarah.Statistics.WebApi.Data;

namespace Sarah.Statistics.WebApi.Services;

public class EnergySampleStore
{
    private readonly StatisticsDbContext _db;

    public EnergySampleStore(StatisticsDbContext db)
    {
        _db = db;
    }

    /// <summary>Persists a raw measurement. Invalid values (NaN, infinite, negative power) are rejected.</summary>
    public async Task<bool> StoreAsync(DeviceEnergyMeasuredMessage message, CancellationToken cancellationToken = default)
    {
        if (double.IsNaN(message.PowerW) || double.IsInfinity(message.PowerW) || message.PowerW < 0)
        {
            return false;
        }

        var kwh = message.EnergyKwhTotal;
        if (kwh.HasValue && (double.IsNaN(kwh.Value) || double.IsInfinity(kwh.Value) || kwh.Value < 0))
        {
            kwh = null;
        }

        var name = message.DeviceName;
        if (name != null && name.Length > 200)
        {
            name = name[..200];
        }

        _db.EnergySamples.Add(new EnergySample
        {
            DeviceId = message.DeviceId,
            DeviceName = name,
            Timestamp = DateTime.SpecifyKind(message.MeasuredAt, DateTimeKind.Utc).ToUniversalTime(),
            PowerW = message.PowerW,
            EnergyKwhTotal = kwh
        });
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
