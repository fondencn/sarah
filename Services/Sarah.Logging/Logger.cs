using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Sarah.Logging
{
    /// <summary>
    /// Logging
    /// </summary>
    public class Logger
    {

        private readonly ILogger _logger;

        public Logger(ILogger<Logger> logger)
        {
            this.LogDebug("ILogger logging system is now injected");
            _logger = logger;
            Instance = this;
        }

        private Logger()
        {
            this.LogDebug("Default logger created. Waiting for ILogger to be injected. ");
            Instance = this;
        }

        public static Logger Instance
        {
            get; private set;
        } = new Logger(); //Default instace while no ILogger is configured via service provider

        /// <summary>
        /// Maximalgröße der in-Memory Log-Queue
        /// </summary>
        private const int MAX_LASTLOG_SIZE = 20;

        public bool LogDebugAsInfo { get; set; } = false;

        /// <summary>
        /// In-Memory Log-Queue
        /// </summary>
        private readonly ConcurrentQueue<string> _lastLog = new System.Collections.Concurrent.ConcurrentQueue<string>();


        /// <summary>
        /// Die letzen MAX_LASTLOG_SIZE logeinträge, die über diesen Logger erzeugt wurden
        /// </summary>
        public string[] LastLogLines => _lastLog.ToArray();


        /// <summary>
        ///
        /// </summary>
        /// <param name="msg"></param>
        public void LogDebug(string msg) => Log(ErrorLevel.Debug, msg);
        /// <summary>
        ///
        /// </summary>
        /// <param name="msg"></param>
        public void LogInfo(string msg) => Log(ErrorLevel.Info, msg);
        /// <summary>
        ///
        /// </summary>
        /// <param name="msg"></param>
        public void LogWarning(string msg) => Log(ErrorLevel.Warning, msg);
        /// <summary>
        ///
        /// </summary>
        /// <param name="msg"></param>
        public void LogError(string msg) => Log(ErrorLevel.Error, msg);
        /// <summary>
        ///
        /// </summary>
        /// <param name="ex"></param>
        public void LogException(Exception ex) => LogException(null, ex);
        /// <summary>
        ///
        /// </summary>
        /// <param name="ex"></param>
        /// <param name="msg"></param>
        public void LogException(string msg, Exception ex) => Log(ErrorLevel.Exception, (msg != null ? (msg + ": ") : "") + ex.Message + ex.StackTrace);

        /// <summary>
        /// LogDebug: Auf Console.out und Debug-out.
        /// </summary>
        /// <param name="msg"></param>
        public void Log(ErrorLevel level, string msg)
        {
            _lastLog.Enqueue(msg);
            if (_lastLog.Count > MAX_LASTLOG_SIZE)
            {
                _lastLog.TryDequeue(out string _);
            }

            if (_logger == null)
            {
                Console.WriteLine(msg);
                Debug.WriteLine(msg);
            }
            else
            {

                switch (level)
                {
                    case ErrorLevel.Debug:
                        if (this.LogDebugAsInfo)
                        {
                            /* Debug als Info loggen in Docker */
                            _logger.LogInformation(msg);
                        }
                        else
                        {
                            _logger.LogDebug(msg);
                        }
                        break;
                    case ErrorLevel.Info:
                        _logger.LogInformation(msg);
                        break;
                    case ErrorLevel.Warning:
                        _logger.LogWarning(msg);
                        break;
                    case ErrorLevel.Error:
                        _logger.LogError(msg);
                        break;
                    case ErrorLevel.Exception:
                        _logger.LogError(msg);
                        break;
                }
            }
        }
    }
}
