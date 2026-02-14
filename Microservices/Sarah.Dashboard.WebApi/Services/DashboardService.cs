using Sarah.Dashboard.WebApi.Data.Entities;
using Sarah.Dashboard.WebApi.Data.Repositories;
using Sarah.Dashboard.WebApi.DTOs;
using System.Text.Json;
using Sarah.API.BusinessObjects;

namespace Sarah.Dashboard.WebApi.Services;

/// <summary>
/// Service interface for dashboard operations
/// </summary>
public interface IDashboardService
{
    Task<IEnumerable<DashboardItemDto>> GetAllDashboardItemsAsync();
    Task<DashboardItemDto?> GetDashboardItemAsync(int id);
    Task<DashboardItemDto> CreateDashboardItemAsync(CreateDashboardItemDto createDto);
    Task<bool> DeleteDashboardItemAsync(int itemId, DashboardItemType itemType);
}

/// <summary>
/// Service for managing dashboard items
/// </summary>
public class DashboardService : IDashboardService
{
    private readonly IRepository<DashboardItemEntity> _repository;
    private readonly ILogger<DashboardService> _logger;

    public DashboardService(
        IRepository<DashboardItemEntity> repository,
        ILogger<DashboardService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<IEnumerable<DashboardItemDto>> GetAllDashboardItemsAsync()
    {
        var entities = await _repository.GetAllAsync();
        return entities.Select(MapToDto);
    }

    public async Task<DashboardItemDto?> GetDashboardItemAsync(int id)
    {
        var entity = await _repository.GetByIdAsync(id);
        return entity != null ? MapToDto(entity) : null;
    }

    public async Task<DashboardItemDto> CreateDashboardItemAsync(CreateDashboardItemDto createDto)
    {
        // Check if item already exists
        var existing = await _repository.FindAsync(
            e => e.ItemId == createDto.ItemId && e.ItemType == createDto.ItemType);
        
        if (existing.Any())
        {
            throw new InvalidOperationException(
                $"Dashboard item with ItemId {createDto.ItemId} and ItemType {createDto.ItemType} already exists.");
        }

        var entity = new DashboardItemEntity
        {
            ItemId = createDto.ItemId,
            ItemType = createDto.ItemType,
            Title = createDto.Title,
            Description = createDto.Description,
            Subtype = createDto.Subtype,
            ExtendedPropertiesJson = createDto.ExtendedProperties != null 
                ? JsonSerializer.Serialize(createDto.ExtendedProperties) 
                : null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var created = await _repository.AddAsync(entity);
        _logger.LogInformation("Created dashboard item: {ItemType} - {ItemId}", created.ItemType, created.ItemId);
        
        return MapToDto(created);
    }

    public async Task<bool> DeleteDashboardItemAsync(int itemId, DashboardItemType itemType)
    {
        var entities = await _repository.FindAsync(
            e => e.ItemId == itemId && e.ItemType == itemType);
        
        var entity = entities.FirstOrDefault();
        if (entity == null)
        {
            return false;
        }

        await _repository.DeleteAsync(entity);
        _logger.LogInformation("Deleted dashboard item: {ItemType} - {ItemId}", itemType, itemId);
        
        return true;
    }

    private static DashboardItemDto MapToDto(DashboardItemEntity entity)
    {
        List<ExtendedPropertyDto>? extendedProperties = null;
        
        if (!string.IsNullOrEmpty(entity.ExtendedPropertiesJson))
        {
            try
            {
                extendedProperties = JsonSerializer.Deserialize<List<ExtendedPropertyDto>>(
                    entity.ExtendedPropertiesJson);
            }
            catch (JsonException)
            {
                // Log error but don't fail the mapping
                extendedProperties = null;
            }
        }

        return new DashboardItemDto
        {
            ItemId = entity.ItemId,
            ItemType = entity.ItemType,
            Title = entity.Title,
            Description = entity.Description,
            Subtype = entity.Subtype,
            ExtendedProperties = extendedProperties
        };
    }
}
