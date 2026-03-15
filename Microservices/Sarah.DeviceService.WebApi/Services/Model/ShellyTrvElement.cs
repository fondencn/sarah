using Microsoft.Extensions.Configuration;
using Sarah.API.Business;
using Sarah.API.BusinessObjects;
using Sarah.API.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Sarah.DeviceService.WebApi.Extensions;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Sarah.DeviceService.Model
{
    /// <summary>
    /// Shelly TRV Gen3 Heizkörperthermostat.
    /// Kommuniziert über die Shelly Gen2+ RPC HTTP API mit einem Shelly BLU Gateway.
    /// </summary>
    public class ShellyTrvElement : ThermoElement
    {
        /// <summary>
        /// Hostname oder IP-Adresse des Shelly BLU Gateways
        /// </summary>
        public string Hostname { get; }

        /// <summary>
        /// Kanal-ID des TRV am Gateway (0-basiert)
        /// </summary>
        public int Channel { get; }

        private static readonly HttpClient _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };

        private Task? _updateTask;
        private CancellationTokenSource? _cts;

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="nodeid">Node-ID im Sarah-System</param>
        /// <param name="hostname">Hostname oder IP des Shelly BLU Gateways</param>
        /// <param name="channel">Kanal-ID des TRV am Gateway (0-basiert)</param>
        /// <param name="publisher">Event publisher</param>
        /// <param name="logger">Optional logger</param>
        public ShellyTrvElement(byte nodeid, string hostname, int channel, NetworkElementPublisher publisher, ILogger? logger = null)
            : base(nodeid, publisher, logger)
        {
            Hostname = hostname;
            Channel = channel;
        }

        /// <summary>
        /// dtor (managed)
        /// </summary>
        ~ShellyTrvElement()
        {
            if (_updateTask != null && _updateTask.Status == TaskStatus.Running)
            {
                _cts?.Cancel();
            }
        }

        public override Task InitializeAsync(IDeviceService deviceService, IConfiguration config)
        {
            // config is intentionally unused; required by the base class contract (NetworkElement.InitializeAsync)
            _logger?.LogInformation("Shelly TRV {NodeId} ({Hostname}, channel {Channel}): start polling status...", NodeID, Hostname, Channel);

            CancellationTokenSource cts = new CancellationTokenSource();
            _cts = cts;
            _updateTask = Task.Run(async () =>
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    await UpdateSensorData();
                    await Task.Delay(5 * 60 * 1000, cts.Token).ConfigureAwait(false); // alle 5 Minuten
                }
            }, cts.Token);

            return Task.CompletedTask;
        }

        private async Task UpdateSensorData()
        {
            try
            {
                string uri = $"http://{Hostname}/rpc/TRV.GetStatus?id={Channel}";
                using HttpResponseMessage response = await _httpClient.GetAsync(uri);
                response.EnsureSuccessStatusCode();
                string json = await response.Content.ReadAsStringAsync();

                using JsonDocument doc = JsonDocument.Parse(json);
                JsonElement root = doc.RootElement;

                if (root.TryGetProperty("current_C", out JsonElement currentC))
                {
                    this.Temperature = new SensorData((float)currentC.GetDouble(), "°C");
                }

                if (root.TryGetProperty("target_C", out JsonElement targetC))
                {
                    this.TemperatureSetpoint = new SensorData((float)targetC.GetDouble(), "°C");
                }

                if (root.TryGetProperty("battery", out JsonElement battery) &&
                    battery.TryGetProperty("percent", out JsonElement batteryPercent))
                {
                    this.Battery = new SensorData((float)batteryPercent.GetDouble(), "%");
                }

                // valve_pos > 0 means heating is active
                if (root.TryGetProperty("valve_pos", out JsonElement valvePos))
                {
                    byte pos = (byte)Math.Min(255, Math.Max(0, valvePos.GetInt32()));
                    this.Basic = new SensorData(pos > 0 ? (float)99 : (float)0, pos > 0 ? "🔥 an" : "❌ aus");
                }
            }
            catch (Exception ex)
            {
                if (!ex.Message.Contains("No route to host") && !ex.Message.Contains("Connection refused"))
                {
                    _logger?.LogWarning("Shelly TRV {NodeId} ({Hostname}, channel {Channel}): status update failed: {Message}",
                        NodeID, Hostname, Channel, ex.Message);
                }
            }
        }

        /// <summary>
        /// Setzt die Zieltemperatur am Shelly TRV via HTTP RPC
        /// </summary>
        public override async Task SetTemperature(float temperature)
        {
            try
            {
                string uri = $"http://{Hostname}/rpc/TRV.SetTarget";
                string body = JsonSerializer.Serialize(new { id = Channel, target_C = temperature });
                using StringContent content = new StringContent(body, Encoding.UTF8, "application/json");

                using HttpResponseMessage response = await _httpClient.PostAsync(uri, content);
                response.EnsureSuccessStatusCode();

                this.TemperatureSetpoint = new SensorData(temperature, "°C");

                await Task.Delay(3000);
                await UpdateSensorData();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Shelly TRV {NodeId} ({Hostname}, channel {Channel}): SetTemperature error",
                    NodeID, Hostname, Channel);
                throw;
            }
        }

        /// <summary>
        /// SetLevel ist beim Shelly TRV nicht direkt unterstützt;
        /// level=0 stellt Frostschutz (5°C), level>0 nimmt die letzte Zieltemperatur wieder auf.
        /// </summary>
        public override async Task SetLevel(byte level)
        {
            if (level == 0)
            {
                await SetTemperature(5.0f); // Frostschutz
            }
            else
            {
                // Keine Aktion nötig – Shelly TRV behält letzte Einstellung
                _logger?.LogDebug("Shelly TRV {NodeId}: SetLevel({Level}) – no direct level control, ignoring", NodeID, level);
            }
        }
    }
}
