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
        private readonly ConcurrentDictionary<int, SemaphoreSlim> _publishLocks = new();

        public TimeSpan MinInterval { get; }
        public TimeSpan HeartbeatInterval { get; }
        public double RelativeChange { get; }

        public EnergyMeasurementThrottle(TimeSpan? minInterval = null, TimeSpan? heartbeatInterval = null, double relativeChange = 0.05)
        {
            MinInterval = minInterval ?? TimeSpan.FromSeconds(30);
            HeartbeatInterval = heartbeatInterval ?? TimeSpan.FromMinutes(5);
            RelativeChange = relativeChange;
        }

        public async Task<bool> PublishIfRequiredAsync(int deviceId, double watts, DateTime now, Func<Task> publish)
        {
            var publishLock = _publishLocks.GetOrAdd(deviceId, _ => new SemaphoreSlim(1, 1));
            await publishLock.WaitAsync();
            try
            {
                if (_last.TryGetValue(deviceId, out var last))
                {
                    var elapsed = now - last.At;
                    if (elapsed < MinInterval)
                    {
                        return false;
                    }

                    var baseline = Math.Max(Math.Abs(last.Watts), 1.0);
                    var changed = Math.Abs(watts - last.Watts) / baseline >= RelativeChange;
                    if (!changed && elapsed < HeartbeatInterval)
                    {
                        return false;
                    }
                }

                await publish();
                _last[deviceId] = (now, watts);
                return true;
            }
            finally
            {
                publishLock.Release();
            }
        }
    }
}
