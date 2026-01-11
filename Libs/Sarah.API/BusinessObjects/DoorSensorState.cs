namespace Sarah.API.BusinessObjects
{
    /// <summary>
    /// Der Zustand eines Tür/Fenster Sensors
    /// </summary>
    public enum DoorSensorState
    {
        /// <summary>
        /// Unbekannt
        /// </summary>
        Unbekannt = 0,

        /// <summary>
        /// Türe geöffnet (sensor getrennt)
        /// </summary>
        Offen,

        /// <summary>
        /// Türe geschlossen (sensor verbunden)
        /// </summary>
        Geschlossen
    }
}
