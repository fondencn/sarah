using Sarah.API.Interfaces.Service;
using Sarah.Logging;
using System.Device.Spi;
using System.Drawing;
using System.Runtime.InteropServices;

namespace Sarah.LEDService
{
    public class ReSpeakerLEDService : ILEDService
    {
        /// <summary>
        /// LED Power on Respeaker Core V2
        /// </summary>
        private const int RESPEAKER_CORE_POWER_PIN = 66; // see MRAA-12 mapping
        /// <summary>
        /// LED Power on Respeaker 4mics HAT on a Raspberry Pi 3b+
        /// </summary>
        private const int POWER_PIN_RASPI = 5; // see MRAA-12 mapping

        /// <summary>
        /// Zugriff auf GPIO
        /// </summary>
        private System.Device.Gpio.GpioController _gpio = new System.Device.Gpio.GpioController();

        /// <summary>
        /// Der GPIO, der die Spannung auf dem LED-Ring steuert (Unterschiedlich zwischen Raspi-Hat und Respeaker-Core-V2)
        /// </summary>
        private int PowerPin => IsOnRaspi ? POWER_PIN_RASPI : RESPEAKER_CORE_POWER_PIN;

        private void Voltage_On()
        {
            System.Device.Gpio.PinValue pinVal = IsOnRaspi ? System.Device.Gpio.PinValue.High : System.Device.Gpio.PinValue.Low;

            if (!_gpio.IsPinOpen(PowerPin))
            {
                _gpio.OpenPin(PowerPin, System.Device.Gpio.PinMode.Output);
                Thread.Sleep(250);
            }
            _gpio.Write(PowerPin, pinVal);

            Logger.Instance.LogDebug("Voltage_On(): GPIO " + PowerPin + " to " + pinVal);
        }

        private void Voltage_Off()
        {
            System.Device.Gpio.PinValue pinVal = IsOnRaspi ? System.Device.Gpio.PinValue.Low : System.Device.Gpio.PinValue.High;

            if (!_gpio.IsPinOpen(PowerPin))
            {
                _gpio.OpenPin(PowerPin, System.Device.Gpio.PinMode.Output);
                Thread.Sleep(250);
            }
            _gpio.Write(PowerPin, pinVal);

            Logger.Instance.LogDebug("Voltage_Off(): GPIO " + PowerPin + " to " + pinVal);
        }

        private bool IsOnRaspi
        {
            get
            {
                bool isRaspi;
                bool isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
                if (isLinux)
                {
                    try
                    {
                        string cpuinfo = File.ReadAllText("/proc/cpuinfo");
                        isRaspi = cpuinfo.Contains("Raspberry Pi", StringComparison.OrdinalIgnoreCase);
                    }
                    catch
                    {
                        isRaspi = false;
                    }
                }
                else
                {
                    isRaspi = false;
                }
                return isRaspi;
            }
        }

        public Task Booting()
        {
            return Task.Run(() =>
            {
                try
                {
                    Voltage_On();
                    Spin(Color.Blue, 1);
                    Spin(Color.Cyan, 1);
                    Spin(Color.Green, 1);
                    Voltage_Off();
                }
                catch (Exception ex)
                {
                    Logger.Instance.LogException("Booting: Fehler beim SPI Zugriff auf den LED Ring: ", ex);
                }
            });
        }


        public Task FlashLight(bool isOn)
        {
            return Task.Run(() =>
            {
                try
                {
                    if (isOn)
                    {
                        Voltage_On();
                        SolidColor(Color.White);
                    }
                    else
                    {
                        Voltage_Off();
                    }
                }
                catch (Exception ex)
                {
                    Logger.Instance.LogException("FlashLight: Fehler beim SPI Zugriff auf den LED Ring: ", ex);
                }
            });
        }

        private static void SolidColor(Color color)
        {
            using (SpiDevice spi = SpiDevice.Create(new SpiConnectionSettings(0)))
            {
                using (Apa102 apa102 = new Apa102(spi, 12))
                {
                    for (var i = 0; i < apa102.Pixels.Length; i++)
                    {
                        apa102.Pixels[i] = color;
                        apa102.Flush();
                        Thread.Sleep(100);
                    }
                }
            }
        }

        private static void Spin(Color color, int count = 1)
        {
            using (SpiDevice spi = SpiDevice.Create(new SpiConnectionSettings(0)))
            {
                using (Apa102 apa102 = new Apa102(spi, 12))
                {
                    for (int c = 0; c < count; c++)
                    {

                        for (var i = 0; i < apa102.Pixels.Length; i++)
                        {
                            for (var j = 0; j < apa102.Pixels.Length; j++)
                            {
                                apa102.Pixels[j] = Color.Black;
                            }

                            if (i > 0)
                            {
                                apa102.Pixels[i - 1] = Color.FromArgb(200, color.R, color.G, color.B);
                            }
                            if (i > 1)
                            {
                                apa102.Pixels[i - 2] = Color.FromArgb(100, color.R, color.G, color.B);
                            }
                            apa102.Pixels[i] = color;
                            apa102.Flush();
                            Thread.Sleep(100);
                        }
                    }
                }
            }
        }

        public Task Listening()
        {
            return Task.Run(() =>
            {
                try
                {
                    Voltage_On();
                    AnimateRandom();
                    Voltage_Off();
                }
                catch (Exception ex)
                {
                    Logger.Instance.LogException("Listening: Fehler beim SPI Zugriff auf den LED Ring: ", ex);
                }
            });
        }

        public Task Speaking()
        {
            return Task.Run(() =>
            {
                try
                {
                    Voltage_On();
                    Spin(Color.Cyan, 3);
                    Voltage_Off();
                }
                catch (Exception ex)
                {
                    Logger.Instance.LogException("Speaking: Fehler beim SPI Zugriff auf den LED Ring: ", ex);
                }
            });
        }

        public Task Thinking()
        {
            return Task.Run(() =>
            {
                try
                {
                    Voltage_On();
                    Spin(Color.Cyan, 3);
                    Spin(Color.Green, 1);
                    Voltage_Off();
                }
                catch (Exception ex)
                {
                    Logger.Instance.LogException("Thinking: Fehler beim SPI Zugriff auf den LED Ring: ", ex);
                }
            });
        }

        private static void AnimateRandom()
        {
            Random random = new Random();

            using (Apa102 apa102 = new Apa102(SpiDevice.Create(new SpiConnectionSettings(0)), 12))
            {

                for (int count = 0; count < 3; count++)
                {
                    for (var i = 0; i < apa102.Pixels.Length; i++)
                    {
                        apa102.Pixels[i] = Color.FromArgb(255, random.Next(256), random.Next(256), random.Next(256));
                    }

                    apa102.Flush();
                    Thread.Sleep(500);
                }
            }
        }

        public void Dispose()
        {
            if (this._gpio != null)
            {
                this.Voltage_Off();
                this._gpio.Dispose();
            }
        }
    }
}
