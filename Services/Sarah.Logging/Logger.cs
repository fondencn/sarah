using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace Sarah.Logging
{
    /// <summary>
    /// Logging
    /// </summary>
    public class Logger
    {
        #region Singleton Pattern
        private static Logger _Instance = null;
        public static Logger Instance
        {
            get
            {
                if(_Instance == null)
                {
                    _Instance = new Logger();
                }
                return _Instance;
            }
        }

        private Logger() { }
        #endregion


        /// <summary>
        /// Maximalgröße der in-Memory Log-Queue
        /// </summary>
        private const int MAX_LASTLOG_SIZE = 20;


        /// <summary>
        /// In-Memory Log-Queue
        /// </summary>
        private readonly ConcurrentQueue<string> _lastLog = new System.Collections.Concurrent.ConcurrentQueue<string>();


        /// <summary>
        /// Die letzen MAX_LASTLOG_SIZE logeinträge, die über diesen Logger erzeugt wurden
        /// </summary>
        public string[] LastLogLines => _lastLog.ToArray();

#if DEBUG
        /// <summary>
        /// Die aktuelle Mindest-Loglevel des Loggers
        /// </summary>
        public ErrorLevel ErrorLevel { get; set; } = ErrorLevel.Debug;
#else
        /// <summary>
        /// Die aktuelle Mindest-Loglevel des Loggers
        /// </summary>
        public ErrorLevel ErrorLevel { get; set; } = ErrorLevel.Info;
#endif

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
            if (level >= ErrorLevel)
            {
                int threadId = Thread.CurrentThread.ManagedThreadId;
                string log = String.Format(CultureInfo.CurrentCulture, "[{0}]\t{1}\t{2}\t{3}", level, DateTime.Now, threadId, msg);
                if (level == ErrorLevel.Debug)
                {
                    Debug.WriteLine(log);
                }
                Console.WriteLine(log);
                this._lastLog.Enqueue(log);
                if (this._lastLog.Count > MAX_LASTLOG_SIZE)
                {
                    this._lastLog.TryDequeue(out string _);
                }
            }
        }
    }
}
