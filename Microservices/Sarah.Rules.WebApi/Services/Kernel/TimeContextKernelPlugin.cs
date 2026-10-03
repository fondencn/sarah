using System.ComponentModel;
using Microsoft.SemanticKernel;

namespace Sarah.Rules.Services.Kernel;

public sealed class TimeContextKernelPlugin
{
    private readonly IConfiguration _configuration;

    public TimeContextKernelPlugin(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [KernelFunction, Description("Liefert aktuellen Zeitkontext mit Datum, Uhrzeit, Wochentag, Wochenende und Ruhezeit.")]
    public string GetCurrentTimeContext()
    {
        var now = DateTime.Now;
        var (startHour, endHour) = GetQuietHours();
        bool weekend = now.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
        bool quietNow = IsInQuietHours(now, startHour, endHour);

        return $"Lokale Zeit: {now:O}. Wochentag: {now:dddd}. Wochenende: {(weekend ? "ja" : "nein")}. " +
               $"Ruhezeit aktiv: {(quietNow ? "ja" : "nein")}. Ruhezeit-Konfiguration: {startHour:00}:00 bis {endHour:00}:00.";
    }

    [KernelFunction, Description("Prueft, ob aktuell Ruhezeit aktiv ist.")]
    public string IsQuietHoursNow()
    {
        var now = DateTime.Now;
        var (startHour, endHour) = GetQuietHours();
        bool quietNow = IsInQuietHours(now, startHour, endHour);
        return quietNow ? "Ja, aktuell ist Ruhezeit." : "Nein, aktuell ist keine Ruhezeit.";
    }

    [KernelFunction, Description("Liefert den naechsten Beginn und das naechste Ende der Ruhezeit.")]
    public string GetNextQuietHoursWindow()
    {
        var now = DateTime.Now;
        var (startHour, endHour) = GetQuietHours();

        DateTime start = new DateTime(now.Year, now.Month, now.Day, startHour, 0, 0);
        DateTime end = new DateTime(now.Year, now.Month, now.Day, endHour, 0, 0);

        if (startHour > endHour)
        {
            end = end.AddDays(1);
        }

        if (now > end)
        {
            start = start.AddDays(1);
            end = end.AddDays(1);
        }
        else if (now > start && now <= end)
        {
            start = start.AddDays(1);
            end = end.AddDays(1);
        }

        return $"Naechste Ruhezeit: Start {start:O}, Ende {end:O}.";
    }

    private (int startHour, int endHour) GetQuietHours()
    {
        int startHour = int.TryParse(_configuration["SilentStartHour"], out int start) ? start : 22;
        int endHour = int.TryParse(_configuration["SilentEndHour"], out int end) ? end : 6;
        return (startHour, endHour);
    }

    private static bool IsInQuietHours(DateTime now, int startHour, int endHour)
    {
        if (startHour == 0 && endHour == 0)
        {
            return false;
        }

        var start = new DateTime(now.Year, now.Month, now.Day, startHour, 0, 0);
        var end = new DateTime(now.Year, now.Month, now.Day, endHour, 0, 0);

        if (startHour > endHour)
        {
            end = end.AddDays(1);
            if (now < start)
            {
                start = start.AddDays(-1);
            }
        }

        return now >= start && now <= end;
    }
}
