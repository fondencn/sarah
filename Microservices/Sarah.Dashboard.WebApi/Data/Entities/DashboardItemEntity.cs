using Sarah.API.BusinessObjects;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sarah.Dashboard.WebApi.Data.Entities;

/// <summary>
/// Entity representing a dashboard item in the database
/// </summary>
[Table("DashboardItems")]
public class DashboardItemEntity
{
    /// <summary>
    /// Primary key for the dashboard item
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    
    /// <summary>
    /// ID of the item in its source system (e.g., device ID, person ID)
    /// </summary>
    [Required]
    public int ItemId { get; set; }
    
    /// <summary>
    /// Type of the dashboard item
    /// </summary>
    [Required]
    public DashboardItemType ItemType { get; set; }
    
    /// <summary>
    /// Title of the dashboard item
    /// </summary>
    [MaxLength(200)]
    public string? Title { get; set; }
    
    /// <summary>
    /// Description of the dashboard item
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }
    
    /// <summary>
    /// Subtype for categorization
    /// </summary>
    [MaxLength(100)]
    public string? Subtype { get; set; }
    
    /// <summary>
    /// When the item was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// When the item was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
