namespace Sarah.Dashboard.WebApi.DTOs;

/// <summary>
/// Data transfer object for reordering dashboard items
/// </summary>
public class ReorderDashboardItemsDto
{
    /// <summary>
    /// Database IDs of dashboard items in the desired display order (first element = position 0)
    /// </summary>
    public List<int> OrderedIds { get; set; } = new();
}
