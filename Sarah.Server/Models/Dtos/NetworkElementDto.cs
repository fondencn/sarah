namespace Sarah.Server.Models.Dtos
{
    /// <summary>
    /// Represents a Data Transfer Object (DTO) for a network element.
    /// </summary>
    public class NetworkElementDto
    {
        /// <summary>
        /// Gets or sets the unique identifier for the network element.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the type of the network element.
        /// </summary>
        public string Type { get; set; } = "";
    }
}