using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sarah.API.BusinessObjects
{
    public static class UnixTime
    {
        /// <summary>
        /// Wandelt die angegebene Unit TIme in ein lokales DateTime Objekt um (UTC Zeit)
        /// </summary>
        /// <param name="unixTimeMillis"></param>
        /// <returns></returns>
        public static DateTime? GetDateTimeFromLinuxEpochMilliseconds(long? unixTimeMillis)
        {
            if (unixTimeMillis.HasValue)
            {
                // Unix timestamp is seconds past epoch
                System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
                dtDateTime = dtDateTime.AddMilliseconds(unixTimeMillis.Value).ToLocalTime();
                return dtDateTime;
            }
            else
            {
                return null;
            }
        }
        /// <summary>
        /// Wandelt die angegebene Unit TIme in ein lokales DateTime Objekt um (UTC Zeit)
        /// </summary>
        /// <param name="unixTimeSeconds"></param>
        /// <returns></returns>
        public static DateTime? GetDateTimeFromLinuxEpochSeconds(long? unixTimeSeconds)
        {
            if (unixTimeSeconds.HasValue)
            {
                // Unix timestamp is seconds past epoch
                System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
                dtDateTime = dtDateTime.AddSeconds(unixTimeSeconds.Value).ToLocalTime();
                return dtDateTime;
            }
            else
            {
                return null;
            }
        }
    }
}
