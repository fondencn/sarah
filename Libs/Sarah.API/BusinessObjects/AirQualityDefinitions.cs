using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sarah.API.BusinessObjects
{
    /// <summary>
    /// Schwellwertdefinitionen für Probleme mit der Atemluft
    /// </summary>
    public enum AirQualitityLevel
    {
        OK, Warning, Bad, SuperBad
    }


    /// <summary>
    /// Definitionen der Luftqualitäts-Schwellwerte
    /// </summary>
    public static class AirQualityDefinitions
    {
        /// <summary>
        /// Luftfeuchtigkeit Levels
        /// </summary>
        private static Dictionary<float, Tuple<AirQualitityLevel, string>> HumidityLevels = new Dictionary<float, Tuple<AirQualitityLevel, string>>()
        {
            { 0,  new Tuple<AirQualitityLevel, string>(AirQualitityLevel.OK, "Die Luftfeuchtigkeit ist in Ordnung")},
            { 60, new Tuple<AirQualitityLevel, string>(AirQualitityLevel.Bad, "Erhöhte Luftfeuchtigkeit")},
            { 80, new Tuple<AirQualitityLevel, string>(AirQualitityLevel.SuperBad, "Die Luftfeuchtigkeit ist zu hoch")},
        };


        /// <summary>
        ///Kohlenstoffdioxyd Levels
        /// </summary>
        private static Dictionary<float, Tuple<AirQualitityLevel, string>> Co2levels = new Dictionary<float, Tuple<AirQualitityLevel, string>>()
        {
            { float.MinValue,    new Tuple<AirQualitityLevel, string>(AirQualitityLevel.OK, "Der Kohlenstoffdioxidanteil konnte nicht ermittelt werden")},
            { 0,    new Tuple<AirQualitityLevel, string>(AirQualitityLevel.OK, "Der Kohlenstoffdioxidanteil ist in Ordnung")},
            { 1400, new Tuple<AirQualitityLevel, string>(AirQualitityLevel.Warning, "Der Kohlenstoffdioxidanteil ist erhöht, bitte bei Gelegenheit lüften")},
            { 2000, new Tuple<AirQualitityLevel, string>(AirQualitityLevel.Bad, "Der Kohlenstoffdioxidanteil ist hoch, bitte sofort lüften")},
            { 3000, new Tuple<AirQualitityLevel, string>(AirQualitityLevel.SuperBad, "Der Kohlenstoffdioxidanteil ist stark erhöht, verstärktes Lüften ist unbedingt notwendig, um Müdigkeit, Schwindel & Konzentrationsschwäche vorzubeugen")},
        };


        /// <summary>
        /// Org. Bestandteile Levels
        /// </summary>
        private static Dictionary<float, Tuple<AirQualitityLevel, string>> VocLevels = new Dictionary<float, Tuple<AirQualitityLevel, string>>()
        {
            { 0f,     new Tuple<AirQualitityLevel, string>(AirQualitityLevel.OK, "Der Anteil organischer Verbindungen ist niedrig")},
            { 1f,     new Tuple<AirQualitityLevel, string>(AirQualitityLevel.Warning, "Der Anteil organischer Verbindungen ist erhöht, bitte bei Gelegenheit lüften")},
            { 1.5f,   new Tuple<AirQualitityLevel, string>(AirQualitityLevel.Bad, "Der Anteil organischer Verbindungen ist stark erhöht, bitte sofort lüften")},
            { 3f,     new Tuple<AirQualitityLevel, string>(AirQualitityLevel.SuperBad, "Der Anteil organischer Verbindungen ist bedenklich, verstärktes Lüften ist unbedingt notwendig")},
        };

        /// <summary>
        ///Gibt die  Luftfeuchtigkeits Warnschwelle zurück, zu der der angegebene Wert gehört
        /// </summary>
        /// <param name="measuredValue">Messwert</param>
        /// <returns>Warnschwellendefinition</returns>
        public static Tuple<AirQualitityLevel,string> GetHumidityLevel(float measuredValue)
            => HumidityLevels.OrderByDescending(item => item.Key).First(item => item.Key <= measuredValue).Value;

        /// <summary>
        ///Gibt die CO² Warnschwelle zurück, zu der der angegebene Wert gehört
        /// </summary>
        /// <param name="measuredValue">Messwert</param>
        /// <returns>Warnschwellendefinition</returns>
        public static Tuple<AirQualitityLevel, string> GetCo2Level(float measuredValue)
            =>  Co2levels.OrderByDescending(item => item.Key).First(item => item.Key <= measuredValue).Value;


        /// <summary>
        ///Gibt die Organische Warnschwelle zurück, zu der der angegebene Wert gehört
        /// </summary>
        /// <param name="measuredValue">Messwert</param>
        /// <returns>Warnschwellendefinition</returns>
        public static Tuple<AirQualitityLevel, string> GetVocLevel(float measuredValue)
            =>  VocLevels.OrderByDescending(item => item.Key).First(item => item.Key <= measuredValue).Value;


    }
}
