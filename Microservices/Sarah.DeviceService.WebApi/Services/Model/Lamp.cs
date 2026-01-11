using Microsoft.Extensions.Configuration;
using Sarah.API.BusinessObjects;
using Sarah.API.Interfaces;
using Sarah.API.Interfaces.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Text;
using System.Threading.Tasks;
using ZWave;
using ZWave.CommandClasses;

namespace Sarah.DeviceService.Model
{
    /// <summary>
    /// Lumière
    /// </summary>
    public class Lamp : NetworkElement, ILamp
    {
        private byte _brightness;
        private string _color;
        private string _meter;
        private IDeviceService _deviceService;

        public override string ClassDescription => "Lampe";

        public override string StateInfo
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("<p>Helligkeit: " + this.Brightness + (IsActive == true ? "💡" : "🌙") + "</p>");
                sb.Append("<p>Farbe: " + this.Color + "</p>");
                sb.Append("<p>Meter: " + this.Meter + "</p>");
                return sb.ToString();
            }
        }

        /// <summary>
        /// Helligkeit (aktuell)
        /// </summary>
        public byte Brightness
        {
            get => _brightness;
            private set { if (_brightness != value) { _brightness = value; ReportEvent(new NetworkEvent<byte>(this.NodeID, value)); } }
        }

        /// <summary>
        /// Farbe (aktuell)
        /// </summary>
        public string Color
        {
            get => _color;
            private set { if (_color != value) { _color = value; if (value != "?") { ReportEvent(new NetworkEvent<string>(this.NodeID, value)); } } }
        }


        /// <summary>
        /// Meter
        /// </summary>
        public string Meter { get => _meter; private set { if (!String.Equals(_meter, value)) { _meter = value; ReportEvent(new NetworkEvent<string>(this.NodeID, value)); } } }

        /// <summary>
        /// Letzte Änderungszeitpunkt
        /// </summary>
        public DateTime? LastChange { get; private set; }

        public override bool? IsActive => this.Brightness > 0;

        public LampColorModes ColorMode { get; private set; }

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="nodeid"></param>
        public Lamp(byte nodeid, LampColorModes mode, IEventProcessingService events) : base(nodeid, events)
        {
            this.ColorMode = mode;
        }

        /// <summary>
        /// Initialisiert die Verbindung mit dem Zwave Gerät / Network Node
        /// </summary>
        public override Task InitializeAsync(IDeviceService deviceService, IConfiguration config = null)
        {
            this._deviceService = deviceService;
            Node n = deviceService.GetNode(this.NodeID) as Node;
            if (n != null)
            {
                Basic basic = n.GetCommandClass<Basic>();
                basic.Changed += (s, e) => { this.Brightness = e.Report.CurrentValue; };


                var meter = n.GetCommandClass<Meter>();
                meter.Changed += OnMeterChanged;

                this.Color = "#FFFFFF";
                this.Brightness = 0;

            }

            return Task.CompletedTask;
        }

        private void OnMeterChanged(object sender, ReportEventArgs<MeterReport> e)
        {
            _logger?.LogDebug($"Meter report of Node {e.Report.Node:D3} changed to [{e.Report.Value}{e.Report.Unit}]");
            this.Meter = e.Report.Value.ToString() + e.Report.Unit;
        }

        /// <summary>
        /// Schaltet die Lampe ganz Hell bzw. aus je nach Zustand (Umschalter)
        /// </summary>
        public Task ToggleState() => this.SetBrightness(this.Brightness > 0 ? (byte)0 : (byte)255);

        /// <summary>
        /// Setzt die Lampe auf eine neue Helligkeit
        /// </summary>
        /// <param name="value">Neuer Helligkeitswert</param>
        /// <returns>Task</returns>
        public async Task SetBrightness(byte value)
        {
            try
            {
                //if (this.Brightness != value)
                //{
                Node n = this._deviceService.GetNode(this.NodeID) as Node;
                //Basic basic = n.GetCommandClass<Basic>();
                //await basic.Set(value);
                SwitchMultiLevel swl = n.GetCommandClass<SwitchMultiLevel>();
                await swl.Set(value);
                this.Brightness = value;
                this.LastChange = DateTime.Now;
                //}
            }
            catch (Exception ex)
            {
                _logger?.LogDebug("Lamp SetBrightness Exception: " + ex.Message);
            }
        }

        public async Task SetWarmWhite()
        {
            try
            {
                Node n = this._deviceService.GetNode(this.NodeID) as Node;
                if (n != null && this.ColorMode == LampColorModes.RGBWW)
                {
                    Color c = n.GetCommandClass<Color>();

                    ColorComponent warmWhite = new ColorComponent(ColorComponentType.WarmWhite, 255);
                    ColorComponent coldWhite = new ColorComponent(ColorComponentType.CoolWhite, 0);
                    ColorComponent r = new ColorComponent(ColorComponentType.Red, 0);
                    ColorComponent g = new ColorComponent(ColorComponentType.Green, 0);
                    ColorComponent b = new ColorComponent(ColorComponentType.Blue, 0);

                    await c.Set(new ColorComponent[] { warmWhite, coldWhite, r, g, b });
                    this.LastChange = DateTime.Now;

                }
                else if (n != null && this.ColorMode == LampColorModes.Mono)
                {
                    Color c = n.GetCommandClass<Color>();

                    ColorComponent warmWhite = new ColorComponent(ColorComponentType.WarmWhite, 255);
                    ColorComponent coldWhite = new ColorComponent(ColorComponentType.CoolWhite, 0);

                    await c.Set(new ColorComponent[] { warmWhite, coldWhite });
                    this.LastChange = DateTime.Now;

                }
            }
            catch (Exception ex)
            {
                _logger?.LogDebug("Fehler beim Lampenfarbe setzen: " + ex.Message);
            }
        }

        public async Task SetColdWhite()
        {
            try
            {
                Node n = this._deviceService.GetNode(this.NodeID) as Node;
                if (n != null && this.ColorMode == LampColorModes.RGBWW)
                {
                    Color c = n.GetCommandClass<Color>();

                    ColorComponent warmWhite = new ColorComponent(ColorComponentType.WarmWhite, 0);
                    ColorComponent coldWhite = new ColorComponent(ColorComponentType.CoolWhite, 255);
                    ColorComponent r = new ColorComponent(ColorComponentType.Red, 0);
                    ColorComponent g = new ColorComponent(ColorComponentType.Green, 0);
                    ColorComponent b = new ColorComponent(ColorComponentType.Blue, 0);

                    await c.Set(new ColorComponent[] { warmWhite, coldWhite, r, g, b });
                    this.LastChange = DateTime.Now;

                }
                else if (n != null && this.ColorMode == LampColorModes.Mono)
                {
                    Color c = n.GetCommandClass<Color>();

                    ColorComponent warmWhite = new ColorComponent(ColorComponentType.WarmWhite, 0);
                    ColorComponent coldWhite = new ColorComponent(ColorComponentType.CoolWhite, 255);

                    await c.Set(new ColorComponent[] { warmWhite, coldWhite });
                    this.LastChange = DateTime.Now;

                }
            }
            catch (Exception ex)
            {
                _logger?.LogDebug("Fehler beim Lampenfarbe setzen: " + ex.Message);
            }
        }
        /// <summary>
        /// Setzt die Lampe auf eine neue Farbe
        /// </summary>
        /// <param name="color">Neuer Farbwert als Hex-String</param>
        /// <returns>Task</returns>
        public async Task SetColor(string color)
        {
            if (!String.Equals(this.Color, color, StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    Node n = this._deviceService.GetNode(this.NodeID) as Node;
                    if (n != null && !String.Equals(this.Color, color, StringComparison.OrdinalIgnoreCase))
                    {
                        {
                            //Basic basicCmd = n.GetCommandClass<Basic>();
                            Color colorCmd = n.GetCommandClass<Color>();
                            System.Drawing.Color cc = ColorConverter.FromHex(color);

                            /* via https://aeotec.freshdesk.com/support/solutions/articles/6000202221-led-bulb-6-multi-color-user-guide-
                             * Switch Color SET Command Class.

                                LED Bulb 6 uses SWITCH COLOR Command Class to allow you to change between Warm White, Cold White, or a mixture of RGB colors. Warm White takes the highest priority and will default to this setting on factory reset values.

                                Capability ID
                                Color
                                0
                                Warm White
                                1
                                Cold White
                                2
                                Red
                                3
                                Green
                                4
                                Blue

                                Notes:
                                Warm white takes highest priority over all other colors.
                                In order for Cold White to appear, Warm White must be disabled or set to 0% intensity
                                For RGB color mixes to work, both Cold White and Warm White must be disabled or set to 0% intensity.
                             *
                             */
                            if (this.ColorMode == LampColorModes.RGBWW)
                            {
                                /* Aeotec LED Bulb COnfiguration */
                                ColorComponent warmWhite = new ColorComponent(ColorComponentType.WarmWhite, 0);
                                ColorComponent coldWhite = new ColorComponent(ColorComponentType.CoolWhite, 0);
                                ColorComponent r = new ColorComponent(ColorComponentType.Red, cc.R);
                                ColorComponent g = new ColorComponent(ColorComponentType.Green, cc.G);
                                ColorComponent b = new ColorComponent(ColorComponentType.Blue, cc.B);
                                await colorCmd.Set(new ColorComponent[] { warmWhite, coldWhite, r, g, b });
                                this.Color = color;
                                this.LastChange = DateTime.Now;
                            }
                            else if (this.ColorMode == LampColorModes.RGBW)
                            {


                                await colorCmd.Set(new ColorComponent[]
                                {
                                /* KOnfigfuration momentan für Fibaro RGBW Controller 2 mit RGB-Ledstrip (kein W-Channel) */
                                new ColorComponent(ColorComponentType.Red ,cc.B),
                                new ColorComponent(ColorComponentType.Green, cc.G),
                                new ColorComponent(ColorComponentType.Blue, cc.R), //ACHTUNG: bei FIBARO LED CONTROLLER FALSCH RUM!!!
                                new ColorComponent(ColorComponentType.Amber, 0), //W
                                });
                                this.Color = color;
                                this.LastChange = DateTime.Now;
                            }
                            else if (this.ColorMode == LampColorModes.RGB)
                            {
                                /* Ungetestete Konfiguration */
                                ColorComponent r = new ColorComponent(ColorComponentType.Red, cc.R);
                                ColorComponent g = new ColorComponent(ColorComponentType.Green, cc.G);
                                ColorComponent b = new ColorComponent(ColorComponentType.Blue, cc.B);
                                await colorCmd.Set(new ColorComponent[] { r, g, b });
                                this.Color = color;
                                this.LastChange = DateTime.Now;
                            }

                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogDebug("Fehler beim Lampenfarbe setzen: " + ex.Message);
                }
            }
        }

        public Animation CurrentAnimation { get; set; }
    }

    public enum LampColorModes
    {
        Mono = 0,
        RGB = 1,
        RGBW = 2,
        RGBWW = 3
    }
}
