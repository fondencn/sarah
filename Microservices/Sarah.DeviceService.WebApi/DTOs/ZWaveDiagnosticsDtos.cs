namespace Sarah.DeviceService.WebApi.DTOs
{
    public enum ZWaveNodeStatus
    {
        Alive,
        Sleeping,
        Failed,
        Unreachable
    }

    public enum ZWaveInclusionMode
    {
        None,
        Inclusion,
        Exclusion
    }

    public class ZWaveNodeDiagnosticsDto
    {
        public byte NodeId { get; set; }
        public ZWaveNodeStatus Status { get; set; }
        public bool IsListening { get; set; }
        public bool IsKnown { get; set; }
        public string? ElementType { get; set; }
        public string? GenericType { get; set; }
        public string? SpecificType { get; set; }
        public int[] Neighbors { get; set; } = [];
        public string? Error { get; set; }
    }

    public class ZWaveNetworkEventDto
    {
        public byte NodeId { get; set; }
        public bool IsNodeAdded { get; set; }
        public bool IsKnown { get; set; }
        public DateTimeOffset Timestamp { get; set; }
    }

    public class ZWaveModeStatusDto
    {
        public bool ControllerAvailable { get; set; }
        public ZWaveInclusionMode Mode { get; set; }
        public DateTimeOffset? ExpiresAt { get; set; }
        public ZWaveNetworkEventDto? LastEvent { get; set; }
    }

    public class ZWaveAssociationGroupDto
    {
        public byte GroupId { get; set; }
        public byte MaxNodesSupported { get; set; }
        public int[] Nodes { get; set; } = [];
    }

    public class ZWaveParameterDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public long Value { get; set; }
        public int Size { get; set; }
    }

    public class ZWaveDeviceParametersDto
    {
        public string DeviceType { get; set; } = string.Empty;
        /// <summary>False when no parameter provider is registered for the device type.</summary>
        public bool HasProvider { get; set; }
        public List<ZWaveParameterDto> Parameters { get; set; } = [];
    }

    public class SetZWaveParameterRequest
    {
        public long Value { get; set; }
    }

    public class SetZWaveAssociationRequest
    {
        public int[] NodeIds { get; set; } = [];
    }
}
