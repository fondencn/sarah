using System;

namespace Sarah.API.BusinessObjects
{
    /// <summary>
    /// Wochentage als Flags-Enum
    /// </summary>
    [Flags]
    public enum Weekdays : long
    {
        Unknown = 0,
        Montag = 1,
        Dienstag = 2,
        Mittwoch = 4,
        Donnerstag = 8,
        Freitag = 16,
        Samstag = 32,
        Sonntag = 64
    }
}
