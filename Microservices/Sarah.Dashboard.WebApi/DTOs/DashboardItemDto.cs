using Sarah.API.BusinessObjects;

namespace Sarah.Dashboard.WebApi.DTOs;

/// <summary>
/// Data transfer object for dashboard items
/// </summary>
public class DashboardItemDto
{
    /// <summary>
    /// Database primary key of the dashboard item
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Unique identifier for the dashboard item
    /// </summary>
    public int ItemId { get; set; }
    
    /// <summary>
    /// Type of the dashboard item (Device, Scene, Room, Person, Weather)
    /// </summary>
    public DashboardItemType ItemType { get; set; }
    
    /// <summary>
    /// Title of the dashboard item
    /// </summary>
    public string? Title { get; set; }
    
    /// <summary>
    /// Description of the dashboard item
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Subtype for more granular categorization (e.g., "lamp", "wallplug")
    /// </summary>
    public string? Subtype { get; set; }
    
    /// <summary>
    /// Extended properties for additional data (key-value pairs)
    /// </summary>
    public List<ExtendedPropertyDto>? ExtendedProperties { get; set; }

    /// <summary>
    /// Display order position (lower values appear first)
    /// </summary>
    public int Position { get; set; }
}
