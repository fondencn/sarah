using System;

namespace Sarah.API.BusinessObjects.SpeakerRequests
{
    public class SetAlarmScheduleRequest
    {
        public DateTime AlarmTime { get; set; }
        public string Text { get; set; } = string.Empty;
        public string TargetSpeaker { get; set; } = string.Empty;
    }
}
