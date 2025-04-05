using System;

namespace Sarah.Voice
{
    /// <summary>
    /// Informationen über den Sprachverlauf
    /// </summary>
    public class SpeechInfo
    {
        /// <summary>
        /// Ein oder Ausgabe
        /// </summary>
        public string Mode { get; set; }

        /// <summary>
        /// Erkannter oder wiedergegebener Text
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Zeitpunkt des Ereignisses
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="mode"></param>
        /// <param name="text"></param>
        public SpeechInfo(string mode, string text) : this(mode, text, DateTime.Now) { }

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="mode"></param>
        /// <param name="text"></param>
        /// <param name="date"></param>
        public SpeechInfo(string mode, string text, DateTime date)
        {
            this.Mode = mode;
            this.Text = text;
            this.Date = date;
        }
    }
}
