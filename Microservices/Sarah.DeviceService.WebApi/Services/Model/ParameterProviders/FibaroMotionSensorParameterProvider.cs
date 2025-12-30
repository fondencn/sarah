using System.Collections.Generic;

namespace Sarah.DeviceService.Model.ParameterProviders
{
    internal class FibaroMotionSensorParameterProvider : AbstractParameterProvider
    {
        private Dictionary<byte, string> InternalParameterNames { get; } = new Dictionary<byte, string>()
        {
            {1, "Empfindlichkeit des Bewegungssensors (8-255). Je niedriger der Wert, desto empfindlicher ist der Sensor." },
            {2, "Trägheit des Bewegungssensors (0-15 s). Der Sensor ist in dieser Zeit blind für weitere Bewegungen. Wert sollte kleiner sein, als der von Paramter 6." },
            {3, "Pulse-Counter des Bewegungssensors. Anzahl an erkannten Bewegungen bis zur Meldung an den Controller (0-3, sollte nicht verändert werden)" },
            {4, "Zeitfenster in Sekunden, in welchem die in 3. definierte Anzahl an Bewegungen erkannt werden müssen." },
            {6, "Zeit ohne Bewegung bis zur Meldung an den Controller, dass keine Bewegung mehr erkannt wird in Sekunden (1-32000)" },
            {8, "Betriebsmodus des Bewegungssensors (0=immer, 1=nur tagsüber, 2=nur nachts)" },
            {12, "Motion report als BASIC CMD Frame (0=On/Off, 1=On, 2=Off)" },
            {14, "Wert von BASIC On (0-255)" },
            {16, "Wert von BASIC Off (0-255)" },
            {40, "Schwellwert (Lux) des Beleuchtungsreports" },
            {42, "Zeit zwischen den weiteren Reports der Lichtstärke" },
            {60, "Schwellwert (zehntel Grad) des Temperaturreports" },
            {62, "Zeit zwischen Temperaturmessungen" },
            {64, "Zeit zwischen Temperaturreports" },
            {66, "Temperaturkorrektur (Offset)" },
        };

        protected override IEnumerable<byte> GetKnownParameters() => InternalParameterNames.Keys;

        protected override string GetParameterName(byte paramId)
        {
            string res;
            if (!InternalParameterNames.TryGetValue(paramId, out res))
            {
                res = null;
            }
            return res;
        }
    }
}
