using Sarah.Dashboard.WebApi.Data.Entities;
using Sarah.Dashboard.WebApi.Data.Repositories;
using Sarah.Dashboard.WebApi.DTOs;
using Sarah.API.BusinessObjects;
using Sarah.ServiceClients;
using System.Globalization;
using DeviceDto = Sarah.API.BusinessObjects.DTOs.DeviceDto;

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
    private readonly DeviceServiceClient _deviceServiceClient;
    private readonly PersonServiceClient _personServiceClient;
    private readonly ILogger<DashboardService> _logger;

    public DashboardService(
        IRepository<DashboardItemEntity> repository,
        DeviceServiceClient deviceServiceClient,
        PersonServiceClient personServiceClient,
        ILogger<DashboardService> logger)
    {
        _repository = repository;
        _deviceServiceClient = deviceServiceClient;
        _personServiceClient = personServiceClient;
        _logger = logger;
    }

    public async Task<IEnumerable<DashboardItemDto>> GetAllDashboardItemsAsync()
    {
        var entities = await _repository.GetAllAsync();
        var mappedItems = await Task.WhenAll(entities.Select(MapToDtoAsync));
        return mappedItems;
    }

    public async Task<DashboardItemDto?> GetDashboardItemAsync(int id)
    {
        var entity = await _repository.GetByIdAsync(id);
        return entity != null ? await MapToDtoAsync(entity) : null;
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

        var subtype = createDto.Subtype;
        if (createDto.ItemType == DashboardItemType.Device)
        {
            var sourceDevice = await _deviceServiceClient.GetDeviceByIdAsync(createDto.ItemId);
            if (sourceDevice != null)
            {
                subtype = ResolveDeviceSubtype(sourceDevice);
            }
        }

        var entity = new DashboardItemEntity
        {
            ItemId = createDto.ItemId,
            ItemType = createDto.ItemType,
            Title = createDto.Title,
            Description = createDto.Description,
            Subtype = subtype,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var created = await _repository.AddAsync(entity);
        _logger.LogInformation("Created dashboard item: {ItemType} - {ItemId}", created.ItemType, created.ItemId);
        
        return await MapToDtoAsync(created);
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

    private async Task<DashboardItemDto> MapToDtoAsync(DashboardItemEntity entity)
    {
        var dto = new DashboardItemDto
        {
            ItemId = entity.ItemId,
            ItemType = entity.ItemType,
            Title = entity.Title,
            Description = entity.Description,
            Subtype = entity.Subtype,
            ExtendedProperties = null
        };

        if (entity.ItemType == DashboardItemType.Device)
        {
            try
            {
                var device = await _deviceServiceClient.GetDeviceByIdAsync(entity.ItemId);
                if (device != null)
                {
                    dto.Title = string.IsNullOrWhiteSpace(dto.Title) ? device.Name : dto.Title;
                    dto.Description = string.IsNullOrWhiteSpace(dto.Description) ? device.Info : dto.Description;
                    dto.Subtype = string.IsNullOrWhiteSpace(dto.Subtype) ? ResolveDeviceSubtype(device) : dto.Subtype;
                    dto.ExtendedProperties = device.ExtendedProperties?.Select(p => new ExtendedPropertyDto
                    {
                        Key = p.Key,
                        Value = p.Value
                    }).ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load live device data for dashboard item {ItemId}", entity.ItemId);
            }
        }
        else if (entity.ItemType == DashboardItemType.Person)
        {
            try
            {
                var person = await _personServiceClient.GetPersonByIdAsync(entity.ItemId);
                if (person != null)
                {
                    dto.Title = person.Name;
                    var description = person.IsAtHome ? "At home" : "Away";
                    if (person.CurrentGeoFence != null)
                    {
                        description = $"{description} · {person.CurrentGeoFence.Name}";
                    }
                    dto.Description = description;
                    dto.Subtype ??= "Person";
                    dto.ExtendedProperties = new List<ExtendedPropertyDto>
                    {
                        new() { Key = "IsAtHome", Value = person.IsAtHome ? "True" : "False" },
                        new() { Key = "CurrentGeoFence", Value = person.CurrentGeoFence?.Name },
                        new() { Key = "GpsTrackerID", Value = person.GPSTrackerID.ToString() }
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load live person data for dashboard item {ItemId}", entity.ItemId);
            }
        }
        else if (entity.ItemType == DashboardItemType.Room)
        {
            try
            {
                dto.Subtype ??= "Room";
                var summary = await _deviceServiceClient.GetRoomSummaryAsync(entity.ItemId);
                if (summary != null)
                {
                    dto.ExtendedProperties = new List<ExtendedPropertyDto>
                    {
                        new() { Key = "AverageTemperature", Value = summary.AverageTemperature?.ToString("F1", CultureInfo.InvariantCulture) ?? "" },
                        new() { Key = "AnyDoorOpen", Value = summary.AnyDoorOpen ? "True" : "False" },
                        new() { Key = "AnyPresence", Value = summary.AnyPresence ? "True" : "False" }
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load live room data for dashboard item {ItemId}", entity.ItemId);
            }
        }

        return dto;
    }

    private static string? ResolveDeviceSubtype(DeviceDto sourceDevice)
    {
        return sourceDevice.DeviceType switch
        {
            KnownDeviceTypes.LoraWanGpsTracker => "LoraWanGpsTracker",
            KnownDeviceTypes.AeotecLedBulb
                or KnownDeviceTypes.AeotecLedBulb6White
                or KnownDeviceTypes.FibaroRGBWController2
                or KnownDeviceTypes.ShellyWifiLamp => "Lampe",
            KnownDeviceTypes.AeotecSmartSwitch7
                or KnownDeviceTypes.FibaroWallPlug
                or KnownDeviceTypes.PoppWallPlug
                or KnownDeviceTypes.WifiWallPlug => "Steckdose",
            KnownDeviceTypes.FibaroHeatController
                or KnownDeviceTypes.AeotecThermostat
                or KnownDeviceTypes.ShellyTrv => "Heizung",
            _ => sourceDevice.TypeName
        };
    }
}
