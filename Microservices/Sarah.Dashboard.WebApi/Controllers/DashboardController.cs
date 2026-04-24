using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sarah.API.BusinessObjects;
using Sarah.Dashboard.WebApi.DTOs;
using Sarah.Dashboard.WebApi.Services;

namespace Sarah.Dashboard.WebApi.Controllers;

/// <summary>
/// Controller for managing dashboard items
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(
        IDashboardService dashboardService,
        ILogger<DashboardController> logger)
    {
        _dashboardService = dashboardService;
        _logger = logger;
    }

    /// <summary>
    /// Get all dashboard items
    /// </summary>
    /// <returns>List of dashboard items</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<DashboardItemDto>>> GetAllDashboardItems()
    {
        var items = await _dashboardService.GetAllDashboardItemsAsync();
        return Ok(items);
    }

    /// <summary>
    /// Get a specific dashboard item by its database ID
    /// </summary>
    /// <param name="id">Database ID of the dashboard item</param>
    /// <returns>Dashboard item</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DashboardItemDto>> GetDashboardItem(int id)
    {
        var item = await _dashboardService.GetDashboardItemAsync(id);
        
        if (item == null)
        {
            return NotFound();
        }

        return Ok(item);
    }

    /// <summary>
    /// Create a new dashboard item
    /// </summary>
    /// <param name="createDto">Dashboard item to create</param>
    /// <returns>Created dashboard item</returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DashboardItemDto>> CreateDashboardItem(
        [FromBody] CreateDashboardItemDto createDto)
    {
        try
        {
            var created = await _dashboardService.CreateDashboardItemAsync(createDto);
            return Created(string.Empty, created);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Reorder dashboard items
    /// </summary>
    /// <param name="reorderDto">Ordered list of dashboard item IDs</param>
    /// <returns>No content if successful</returns>
    [HttpPut("reorder")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> ReorderDashboardItems([FromBody] ReorderDashboardItemsDto reorderDto)
    {
        var success = await _dashboardService.ReorderDashboardItemsAsync(reorderDto);
        
        if (!success)
        {
            return BadRequest("One or more item IDs were not found.");
        }

        return NoContent();
    }

    /// <summary>
    /// Delete a dashboard item by its ItemId and ItemType
    /// </summary>
    /// <param name="itemId">ID of the item in its source system</param>
    /// <param name="itemType">Type of the dashboard item</param>
    /// <returns>No content if successful</returns>
    [HttpDelete("{itemId}/{itemType}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteDashboardItem(int itemId, DashboardItemType itemType)
    {
        var deleted = await _dashboardService.DeleteDashboardItemAsync(itemId, itemType);
        
        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
}
