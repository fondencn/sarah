namespace Sarah.API.Interfaces.Services {
    public interface IDoorMonitor
    {
        bool IsWindowForHeatingTracking(byte nodeID);
        void UpdateTargetTemperature(byte nodeID, byte temperatureSetpoint);
    }
}