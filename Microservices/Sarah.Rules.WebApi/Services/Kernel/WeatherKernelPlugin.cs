using System.ComponentModel;
using Microsoft.SemanticKernel;
using Sarah.API.Interfaces;

namespace Sarah.Rules.Services.Kernel;

public sealed class WeatherKernelPlugin
{
    private readonly IWeatherProvider _weatherProvider;

    public WeatherKernelPlugin(IWeatherProvider weatherProvider)
    {
        _weatherProvider = weatherProvider;
    }

    [KernelFunction, Description("Liefert die aktuellen Wetterdaten inklusive Temperatur, Wettertext und Tagesvorhersage. Außerdem der Zeitpunkt des Sonnenaufgangs und Sonnenuntergangs.")]
    public string GetCurrentWeather()
    {
        var currentWeather = _weatherProvider.GetCurrentWeatherString();
        var currentTemperature = _weatherProvider.CurrentOutdoorTemperature;
        var averageNext4Hours = _weatherProvider.AverageTemperatureNext4Hours;
        var sunrise = _weatherProvider.GetSunrise();
        var sunset = _weatherProvider.GetSunset();
        var forecastToday = _weatherProvider.GetWeatherForecastStringForToday();

        var averageText = averageNext4Hours.HasValue
            ? $"{averageNext4Hours.Value:F1}"
            : "nicht verfuegbar";
        var sunriseText = sunrise.HasValue
            ? sunrise.Value.ToString("O")
            : "nicht verfuegbar";
        var sunsetText = sunset.HasValue
            ? sunset.Value.ToString("O")
            : "nicht verfuegbar";

        return $"Temperatur aktuell: {currentTemperature:F1} Grad C. " +
               $"Durchschnitt naechste 4 Stunden: {averageText}. " +
               $"Sonnenaufgang: {sunriseText}. " +
               $"Sonnenuntergang: {sunsetText}. " +
               $"Aktuelles Wetter: {currentWeather}. " +
               $"Vorhersage heute: {forecastToday}";
    }

    [KernelFunction, Description("Liefert aktuelle Wetterwarnungen.")]
    public string GetCurrentWarnings()
    {
        var warnings = _weatherProvider.GetWeatherWarningString();
        return string.IsNullOrWhiteSpace(warnings)
            ? "Keine aktuellen Wetterwarnungen vorhanden."
            : warnings;
    }
}
