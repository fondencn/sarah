namespace Sarah.Dashboard.WebApi.DTOs;

/// <summary>
/// Represents a key-value pair for extended properties
/// </summary>
public class ExtendedPropertyDto
{
    /// <summary>
    /// Property key
    /// </summary>
    public string? Key { get; set; }
    
    /// <summary>
    /// Property value
    /// </summary>
    public string? Value { get; set; }
}
