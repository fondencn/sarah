using Sarah.API.BusinessObjects;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Sarah.API.Interfaces
{
    public interface IWeatherProvider
    {
        double CurrentOutdoorTemperature { get; }

        string GetCurrentWeatherString(bool addDebugOutput = false, bool getWarningDetails = false);
        string GetWeatherForecastStringForToday();
        string GetWeatherForecastString(DateTime dteDate);
        string GetWeatherWarningString();
        DateTime? GetSunrise();
        DateTime? GetSunset();
        double? AverageTemperatureNext4Hours { get; }
    }


}
