namespace Sarah.Server.Models.Dtos
{
    /// <summary>
    /// Represents the base data transfer object (DTO) containing various enumerations and collections
    /// related to device types, rooms, and network elements.
    /// </summary>
    public class BaseDataDto
    {
        /// <summary>
        /// Gets or sets the enumeration of device types.
        /// </summary>
        public EnumDto[] DeviceTypeEnumeration { get; set; } = [];

        /// <summary>
        /// Gets or sets the collection of rooms.
        /// </summary>
        public RoomDto[] Rooms { get; set; } = [];

        /// <summary>
        /// Gets or sets the collection of network elements.
        /// </summary>
        public NetworkElementDto[] NetworkElements { get; set; } = [];
    }
}