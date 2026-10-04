using System.Collections.Concurrent;

namespace Sarah.DeviceService.WebApi.Extensions
{
    /// <summary>
    /// Limits the rate of energy measurement events per device: a value is let through when at least
    /// <see cref="MinInterval"/> has passed and it changed noticeably, or when <see cref="HeartbeatInterval"/> has passed.
    /// </summary>
    public class EnergyMeasurementThrottle
    {
        private readonly ConcurrentDictionary<int, (DateTime At, double Watts)> _last = new();

        public TimeSpan MinInterval { get; }
        public TimeSpan HeartbeatInterval { get; }
        public double RelativeChange { get; }

        public EnergyMeasurementThrottle(TimeSpan? minInterval = null, TimeSpan? heartbeatInterval = null, double relativeChange = 0.05)
        {
            MinInterval = minInterval ?? TimeSpan.FromSeconds(30);
            HeartbeatInterval = heartbeatInterval ?? TimeSpan.FromMinutes(5);
            RelativeChange = relativeChange;
        }

        public bool ShouldPublish(int deviceId, double watts, DateTime now)
        {
            while (true)
            {
                if (!_last.TryGetValue(deviceId, out var last))
                {
                    if (_last.TryAdd(deviceId, (now, watts))) return true;
                    continue;
                }

                var elapsed = now - last.At;
                if (elapsed < MinInterval) return false;

                var baseline = Math.Max(Math.Abs(last.Watts), 1.0);
                var changed = Math.Abs(watts - last.Watts) / baseline >= RelativeChange;
                if (!changed && elapsed < HeartbeatInterval) return false;

                if (_last.TryUpdate(deviceId, (now, watts), last)) return true;
            }
        }
    }
}
