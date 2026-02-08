using Sarah.API.BusinessObjects;

namespace Sarah.Dashboard.WebApi.DTOs;

/// <summary>
/// Data transfer object for creating a new dashboard item
/// </summary>
public class CreateDashboardItemDto
{
    /// <summary>
    /// ID of the item in its source system (e.g., device ID, person ID)
    /// </summary>
    public int ItemId { get; set; }
    
    /// <summary>
    /// Type of the dashboard item
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
    /// Subtype for categorization
    /// </summary>
    public string? Subtype { get; set; }
    
    /// <summary>
    /// Extended properties
    /// </summary>
    public List<ExtendedPropertyDto>? ExtendedProperties { get; set; }
}
