using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ZWave.Channel;

namespace Sarah.DeviceService.Model
{
    internal class SerialPortFactory
    {
        #region singleton pattern
        private static SerialPortFactory _Instance = null!;
        public static SerialPortFactory Instance
        {
            get
            {
                if (_Instance == null)
                {
                    _Instance = new SerialPortFactory();
                }
                return _Instance;
            }
        }
        private SerialPortFactory() { }
        #endregion

        /// <summary>
        /// Erstellt eine neue SerialPort Implementierung für die angegebene serielle Schnittstelle
        /// </summary>
        /// <param name="serialPortName"></param>
        /// <returns></returns>
        internal ISerialPort Create(string serialPortName)
        {
            if (String.IsNullOrWhiteSpace(serialPortName))
            {
                throw new ArgumentNullException(nameof(serialPortName));
            }

            serialPortName = serialPortName.Trim(' ', '\r', '\n', '\t');
            string[] availablePorts = System.IO.Ports.SerialPort.GetPortNames();

            if (!availablePorts.Any(item => String.Equals(item, serialPortName, StringComparison.Ordinal)))
            {
                throw new NotSupportedException("Es wurde keine serielle Schnittstelle mit dem Bezeichner \"" + serialPortName + "\" im System gefunden");
            }

            ISerialPort serialPort = new InteLukSerialPort(serialPortName);
            return serialPort;
        }
    }

    /// <summary>
    /// Wrapper für den Microsoft dotnetcore SerialPort für die Weitergabe an ZWave4Net
    /// </summary>
    internal class InteLukSerialPort : ISerialPort, IDisposable
    {
        private const int BUFFER_SIZE = 1;

        /// <summary>
        /// Der gerwappte Port
        /// </summary>
        private System.IO.Ports.SerialPort SerialPort { get; set; }

        private readonly string _serialPortName;

        private bool _isRunning;

        /// <summary>
        /// Gibt an ob de Serialport geöffnet ist
        /// </summary>
        public bool IsRunning => _isRunning;

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="serialPortName">Name des COM Ports</param>
        public InteLukSerialPort(string serialPortName)
        {
            this._serialPortName = serialPortName;
        }

        ///// <summary>
        ///// 
        ///// </summary>
        //private async void StartReading()
        //{
        //    byte[] buffer = new byte[BUFFER_SIZE];
        //    while (this._isRunning)
        //    {
        //        try
        //        {
        //            int count = this.SerialPort.Read(buffer, 0, buffer.Length);
        //            if (count > 0)
        //            {
        //                await this.InputStream.WriteAsync(buffer, 0, count);
        //            }

        //        }
        //        catch (Exception ex)
        //        {
        //            _logger?.LogDebug(ex.Message);
        //            await Task.Delay(1000);
        //        }
        //    }
        //}

        ///// <summary>
        ///// 
        ///// </summary>
        //private async void StartWriting()
        //{
        //    byte[] buffer = new byte[1];

        //    while (this._isRunning)
        //    {
        //        try
        //        {
        //            int count = await this.InputStream.ReadAsync(buffer, 0, buffer.Length);
        //            if (count > 0)
        //            {
        //                this.SerialPort.Write(buffer, 0, count);
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            _logger?.LogDebug(ex.Message);

        //            await Task.Delay(1000);
        //        }
        //    }
        //}

        #region ISerialPort
        public Stream InputStream => SerialPort.IsOpen ? SerialPort.BaseStream : null;

        public Stream OutputStream => SerialPort.IsOpen ? SerialPort.BaseStream : null;

        public void Close()
        {
            this._isRunning = false;
            this.SerialPort.Close();
        }

        public void Open()
        {

            System.IO.Ports.SerialPort serialPort = new System.IO.Ports.SerialPort(_serialPortName, 115200, System.IO.Ports.Parity.None, 8);
            this.SerialPort = serialPort;

            this.SerialPort.Open();
            this._isRunning = true;
            //Task.Run(StartReading);
            //Task.Run(StartWriting);
        }

        #endregion

        #region IDisposable
        public void Dispose()
        {
            if (this.SerialPort != null)
            {
                this.SerialPort.Dispose();
                this.SerialPort = null;
            }
            this._isRunning = false;
        }
        #endregion
    }
}