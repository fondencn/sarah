using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sarah.API.Interfaces.Services;
using Sarah.DeviceService.WebApi.Data;
using Sarah.DeviceService.WebApi.DTOs;
using Sarah.API.BusinessObjects.DTOs;
using Microsoft.EntityFrameworkCore;
using Sarah.API.BusinessObjects;
using System.Linq;
using Sarah.API.Interfaces;

namespace Sarah.DeviceService.WebApi.Controllers;

[Authorize]
[ApiController]
[Route("[controller]")]
[Route("api/[controller]")]
public class DevicesController : ControllerBase
{
    private readonly IDeviceService _deviceService;
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<DevicesController> _logger;

    public DevicesController(IDeviceService deviceService, ApplicationDbContext dbContext, ILogger<DevicesController> logger)
    {
        _deviceService = deviceService;
        _dbContext = dbContext;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        try
        {
            var devices = await _dbContext.Devices.ToListAsync(cancellationToken);
            var dtos = devices.Select(d => new DeviceDto
            {
                Id = d.Id,
                RoomId = d.Id_Room,
                Name = d.Name,
                NodeId = d.NodeID,
                DeviceType = d.SpecificType,
                TypeName = ResolveTypeName(d.SpecificType),
                Info = _deviceService.GetNetworkItem(d.NodeID)?.StateInfo,
                IsReadonly = d.IsReadonly,
                IsFavourite = d.IsFavourite,
                DoorSensor = BuildDoorSensorStateDto(d.NodeID),
                Thermostat = BuildThermoStateDto(d.NodeID),
                AirQuality = BuildAirQualityStateDto(d.NodeID),
                Battery = BuildBatteryStateDto(d.NodeID),
                Lamp = BuildLampStateDto(d.NodeID),
                WallPlug = BuildWallPlugStateDto(d.NodeID)
            }).ToList();
            return Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all devices");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPut]
    public async Task<IActionResult> Create([FromBody] DeviceDto? deviceDto, CancellationToken cancellationToken)
    {
        var validationError = ValidateDeviceDto(deviceDto);
        if (validationError != null) return validationError;
        try
        {
            var entity = new Data.Entities.DeviceInfoEntity
            {
                Name = deviceDto!.Name,
                NodeID = (byte)deviceDto.NodeId,
                Id_Room = deviceDto.RoomId,
                SpecificType = deviceDto.DeviceType,
                IsReadonly = deviceDto.IsReadonly,
                IsFavourite = deviceDto.IsFavourite
            };
            _dbContext.Devices.Add(entity);
            await _dbContext.SaveChangesAsync(cancellationToken);
            deviceDto.Id = entity.Id;
            deviceDto.TypeName = ResolveTypeName(entity.SpecificType);
            return Ok(deviceDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating device");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost]
    public async Task<IActionResult> Update([FromBody] DeviceDto? deviceDto, CancellationToken cancellationToken)
    {
        var validationError = ValidateDeviceDto(deviceDto);
        if (validationError != null) return validationError;
        try
        {
            var entity = await _dbContext.Devices.FirstOrDefaultAsync(d => d.Id == deviceDto!.Id, cancellationToken);
            if (entity == null)
            {
                return NotFound();
            }
            entity.Name = deviceDto!.Name;
            entity.NodeID = (byte)deviceDto.NodeId;
            entity.Id_Room = deviceDto.RoomId;
            entity.SpecificType = deviceDto.DeviceType;
            entity.IsReadonly = deviceDto.IsReadonly;
            entity.IsFavourite = deviceDto.IsFavourite;
            await _dbContext.SaveChangesAsync(cancellationToken);
            deviceDto.TypeName = ResolveTypeName(entity.SpecificType);
            return Ok(deviceDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating device {DeviceId}", deviceDto!.Id);
            return StatusCode(500, "Internal server error");
        }
    }

    private BadRequestObjectResult? ValidateDeviceDto(DeviceDto? deviceDto)
    {
        if (deviceDto == null || string.IsNullOrWhiteSpace(deviceDto.Name))
            return BadRequest("Device name is required");
        if (deviceDto.NodeId < 0 || deviceDto.NodeId > 255)
            return BadRequest("NodeId must be between 0 and 255");
        return null;
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
    {
        try
        {
            var entity = await _dbContext.Devices.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
            if (entity == null)
            {
                return NotFound();
            }
            _dbContext.Devices.Remove(entity);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting device {DeviceId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("lamps")]
    public IActionResult GetLamps()
    {
        try
        {
            var lamps = _deviceService.Lamps.Select(l => l.ToDto()).ToList();
            return Ok(lamps);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving lamps");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("sensors")]
    public IActionResult GetSensors()
    {
        try
        {
            var sensors = _deviceService.Sensors.Select(s => s.ToDto()).ToList();
            return Ok(sensors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving sensors");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("doorsensors")]
    public IActionResult GetDoorSensors()
    {
        try
        {
            var doorSensors = _deviceService.DoorSensors.Select(d => d.ToDto()).ToList();
            return Ok(doorSensors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving door sensors");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("wallplugs")]
    public IActionResult GetWallPlugs()
    {
        try
        {
            var wallPlugs = _deviceService.WallPlugs.Select(w => w.ToDto()).ToList();
            return Ok(wallPlugs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving wall plugs");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("thermostats")]
    public IActionResult GetThermostats()
    {
        try
        {
            var thermostats = _deviceService.Heatings.Select(h => h.ToDto()).ToList();
            return Ok(thermostats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving thermostats");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("controllers")]
    public IActionResult GetControllers()
    {
        try
        {
            var controllers = _deviceService.Controllers.Select(c => c.ToDto()).ToList();
            return Ok(controllers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving controllers");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("wallcontrollers")]
    public IActionResult GetWallControllers()
    {
        try
        {
            var wallControllers = _deviceService.WallControllers.Select(w => w.ToDto()).ToList();
            return Ok(wallControllers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving wall controllers");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("smokesensors")]
    public IActionResult GetSmokeSensors()
    {
        try
        {
            var smokeSensors = _deviceService.SmokeSensors.Select(s => s.ToDto()).ToList();
            return Ok(smokeSensors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving smoke sensors");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("batterysensors")]
    public IActionResult GetBatterySensors()
    {
        try
        {
            var batterySensors = _deviceService.BatterySensors.Select(b => b.ToDto()).ToList();
            return Ok(batterySensors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving battery sensors");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("unknown")]
    public IActionResult GetUnknownElements()
    {
        try
        {
            var unknowns = _deviceService.UnknownElements.Select(u => u.ToDto()).ToList();
            return Ok(unknowns);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving unknown elements");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("gps-trackers")]
    public IActionResult GetGpsTrackers()
    {
        try
        {
            var trackers = _deviceService.GPSTrackers.Select(t => t.ToDto()).ToList();
            return Ok(trackers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving GPS trackers");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("status")]
    [AllowAnonymous]
    public IActionResult GetStatus()
    {
        return Ok(new { status = "running", message = _deviceService.StatusMessage });
    }

    [HttpGet("serial-port")]
    public IActionResult GetSerialPortName()
    {
        try
        {
            return Ok(_deviceService.SerialPortName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving serial port name");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("elements")]
    public IActionResult GetNetworkElements()
    {
        try
        {
            var elements = _deviceService.Elements.Select(e => new
            {
                id = (int)e.NodeID,
                type = e.ClassDescription ?? e.StateInfo ?? e.NodeID.ToString()
            }).ToList();
            return Ok(elements);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving network elements");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("trackers")]
    public IActionResult GetTrackers()
    {
        try
        {
            var trackers = _deviceService.GPSTrackers.Select(t => new
            {
                id = (int)t.NodeID,
                name = t.ClassDescription ?? t.StateInfo ?? $"Tracker {t.NodeID}"
            }).ToList();
            return Ok(trackers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving trackers");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetDeviceById(long id)
    {
        try
        {
            var device = await _dbContext.Devices.FirstOrDefaultAsync(d => d.Id == id);
            if (device == null)
            {
                return NotFound();
            }

            var dto = new DeviceDto
            {
                Id = device.Id,
                RoomId = device.Id_Room,
                Name = device.Name,
                NodeId = device.NodeID,
                DeviceType = device.SpecificType,
                TypeName = ResolveTypeName(device.SpecificType),
                IsReadonly = device.IsReadonly,
                IsFavourite = device.IsFavourite,
                DoorSensor = BuildDoorSensorStateDto(device.NodeID),
                Thermostat = BuildThermoStateDto(device.NodeID),
                AirQuality = BuildAirQualityStateDto(device.NodeID),
                Battery = BuildBatteryStateDto(device.NodeID),
                Lamp = BuildLampStateDto(device.NodeID),
                WallPlug = BuildWallPlugStateDto(device.NodeID),
                ExtendedProperties = BuildExtendedProperties(device.NodeID)
            };

            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving device {DeviceId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    private List<ExtendedPropertyDto> BuildExtendedProperties(byte nodeId)
    {
        var props = new List<ExtendedPropertyDto>();
        var networkItem = _deviceService.GetNetworkItem(nodeId);
        if (networkItem is ILamp lamp)
        {
            props.Add(new ExtendedPropertyDto { Key = "IsOn", Value = (lamp.Brightness > 0).ToString() });
            props.Add(new ExtendedPropertyDto { Key = "Color", Value = lamp.Color });
            props.Add(new ExtendedPropertyDto { Key = "Brightness", Value = lamp.Brightness.ToString() });
        }
        else if (networkItem is IWallPlug wallPlug)
        {
            props.Add(new ExtendedPropertyDto { Key = "IsOn", Value = wallPlug.IsOn.ToString() });
            if (networkItem is Sarah.DeviceService.Model.WallPlug concreteWallPlug && concreteWallPlug.Meter_W != null)
            {
                props.Add(new ExtendedPropertyDto { Key = "Meter_W", Value = concreteWallPlug.Meter_W.Value.ToString() });
            }
        }
        else if (networkItem is IThermoElement thermoElement)
        {
            if (networkItem is ITemperatureSensor tempSensor && tempSensor.Temperature != null)
                props.Add(new ExtendedPropertyDto { Key = "Temperature", Value = tempSensor.Temperature.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) });
            if (thermoElement.TemperatureSetpoint != null)
                props.Add(new ExtendedPropertyDto { Key = "TemperatureSetpoint", Value = thermoElement.TemperatureSetpoint.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) });
            if (networkItem is IBatterySensor batterySensor && batterySensor.Battery != null)
                props.Add(new ExtendedPropertyDto { Key = "Battery", Value = batterySensor.Battery.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) });
        }
        else if (networkItem is IDoorSensor doorSensor)
        {
            props.Add(new ExtendedPropertyDto { Key = "IsOpen", Value = (doorSensor.State == DoorSensorState.Offen).ToString() });
            if (doorSensor.LastOpenDuration.HasValue)
                props.Add(new ExtendedPropertyDto { Key = "LastOpenDurationMinutes", Value = doorSensor.LastOpenDuration.Value.TotalMinutes.ToString("F1", System.Globalization.CultureInfo.InvariantCulture) });
            if (doorSensor.LastStateChanged.HasValue)
                props.Add(new ExtendedPropertyDto { Key = "LastStateChanged", Value = doorSensor.LastStateChanged.Value.ToString("o") });
        }
        else if (networkItem is IMultiSensor multiSensorItem)
        {
            if (networkItem is ITemperatureSensor tempSensorItem && tempSensorItem.Temperature != null)
                props.Add(new ExtendedPropertyDto { Key = "Temperature", Value = tempSensorItem.Temperature.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) });
            if (multiSensorItem.RelativeHumidity != null)
                props.Add(new ExtendedPropertyDto { Key = "RelativeHumidity", Value = multiSensorItem.RelativeHumidity.Value.ToString("F1", System.Globalization.CultureInfo.InvariantCulture) });
            if (multiSensorItem.CO2 != null)
                props.Add(new ExtendedPropertyDto { Key = "CO2", Value = multiSensorItem.CO2.Value.ToString("F0", System.Globalization.CultureInfo.InvariantCulture) });
            if (multiSensorItem.VolatileOrganicCompounds != null)
                props.Add(new ExtendedPropertyDto { Key = "VOC", Value = multiSensorItem.VolatileOrganicCompounds.Value.ToString("F0", System.Globalization.CultureInfo.InvariantCulture) });
        }
        return props;
    }

    private DoorSensorStateDto? BuildDoorSensorStateDto(byte nodeId)
    {
        if (_deviceService.GetNetworkItem(nodeId) is IDoorSensor doorSensor)
        {
            return new DoorSensorStateDto
            {
                State = doorSensor.State,
                LastStateChanged = doorSensor.LastStateChanged
            };
        }
        return null;
    }

    private ThermoStateDto? BuildThermoStateDto(byte nodeId)
    {
        if (_deviceService.GetNetworkItem(nodeId) is IThermoElement thermo)
        {
            return new ThermoStateDto
            {
                TemperatureSetpoint = thermo.TemperatureSetpoint?.Value
            };
        }
        return null;
    }

    private AirQualityStateDto? BuildAirQualityStateDto(byte nodeId)
    {
        if (_deviceService.GetNetworkItem(nodeId) is IMultiSensor sensor)
        {
            return new AirQualityStateDto
            {
                CO2 = sensor.CO2?.Value,
                VolatileOrganicCompounds = sensor.VolatileOrganicCompounds?.Value,
                RelativeHumidity = sensor.RelativeHumidity?.Value
            };
        }
        return null;
    }

    private BatteryStateDto? BuildBatteryStateDto(byte nodeId)
    {
        if (_deviceService.GetNetworkItem(nodeId) is IBatterySensor sensor)
        {
            return new BatteryStateDto
            {
                Level = sensor.Battery?.Value
            };
        }
        return null;
    }

    private LampStateDto? BuildLampStateDto(byte nodeId)
    {
        if (_deviceService.GetNetworkItem(nodeId) is ILamp lamp)
        {
            return new LampStateDto
            {
                Brightness = lamp.Brightness,
                Color = lamp.Color,
                LastChange = lamp.LastChange
            };
        }
        return null;
    }

    private WallPlugStateDto? BuildWallPlugStateDto(byte nodeId)
    {
        if (_deviceService.GetNetworkItem(nodeId) is IWallPlug wp)
        {
            return new WallPlugStateDto
            {
                IsOn = wp.IsOn,
                LastChangeToPowerLow = wp.LastChangeToPowerLow,
                LastChangeToPowerHigh = wp.LastChangeToPowerHigh
            };
        }
        return null;
    }

    private const string LampDeviceSubtype = "Lampe";
    private const string WallPlugDeviceSubtype = "Steckdose";
    private const string HeatingDeviceSubtype = "Heizung";
    private const string DoorSensorDeviceSubtype = "DoorSensor";
    private const string AirQualitySensorDeviceSubtype = "EutronicAirQualitySensor";
    private const string GpsTrackerDeviceSubtype = "LoraWanGpsTracker";

    private static string ResolveTypeName(KnownDeviceTypes deviceType) => deviceType switch
    {
        KnownDeviceTypes.AeotecLedBulb
            or KnownDeviceTypes.AeotecLedBulb6White
            or KnownDeviceTypes.FibaroRGBWController2
            or KnownDeviceTypes.ShellyWifiLamp => LampDeviceSubtype,
        KnownDeviceTypes.AeotecSmartSwitch7
            or KnownDeviceTypes.FibaroWallPlug
            or KnownDeviceTypes.PoppWallPlug
            or KnownDeviceTypes.WifiWallPlug => WallPlugDeviceSubtype,
        KnownDeviceTypes.FibaroHeatController
            or KnownDeviceTypes.AeotecThermostat
            or KnownDeviceTypes.ShellyTrv => HeatingDeviceSubtype,
        KnownDeviceTypes.AeotecDoorSensor
            or KnownDeviceTypes.FibaroDoorWindowSensor2 => DoorSensorDeviceSubtype,
        KnownDeviceTypes.EutronicAirQualitySensor => AirQualitySensorDeviceSubtype,
        KnownDeviceTypes.LoraWanGpsTracker => GpsTrackerDeviceSubtype,
        _ => deviceType.ToString()
    };

    [HttpGet("room/{roomId}/avgtemperature")]
    public async Task<IActionResult> GetRoomAverageTemperature(long roomId, CancellationToken cancellationToken)
    {
        try
        {
            var devices = await _dbContext.Devices
                .Where(d => d.Id_Room == roomId)
                .ToListAsync(cancellationToken);

            var temps = devices
                .Select(d => _deviceService.GetNetworkItem(d.NodeID))
                .OfType<ITemperatureSensor>()
                .Where(s => s.Temperature != null && s.Temperature.Value != 0) // Filter out invalid 0 values
                .Select(s => s.Temperature.Value)
                .ToList();

            if (!temps.Any())
                return NoContent();

            return Ok(CalculateAverageRoomTemperature(temps));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving average temperature for room {RoomId}", roomId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("bynode/{nodeId}")]
    public async Task<IActionResult> GetDeviceByNodeId(byte nodeId, CancellationToken cancellationToken)
    {
        try
        {
            var device = await _dbContext.Devices.FirstOrDefaultAsync(d => d.NodeID == nodeId, cancellationToken);
            if (device == null)
            {
                return NotFound();
            }

            var dto = new DeviceDto
            {
                Id = device.Id,
                RoomId = device.Id_Room,
                Name = device.Name,
                NodeId = device.NodeID,
                DeviceType = device.SpecificType,
                TypeName = ResolveTypeName(device.SpecificType),
                IsReadonly = device.IsReadonly,
                IsFavourite = device.IsFavourite,
                DoorSensor = BuildDoorSensorStateDto(device.NodeID),
                Thermostat = BuildThermoStateDto(device.NodeID),
                AirQuality = BuildAirQualityStateDto(device.NodeID),
                Battery = BuildBatteryStateDto(device.NodeID),
                Lamp = BuildLampStateDto(device.NodeID),
                WallPlug = BuildWallPlugStateDto(device.NodeID)
            };

            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving device for node {NodeId}", nodeId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("node/{nodeId}")]
    public IActionResult GetNode(byte nodeId)
    {
        try
        {
            var node = _deviceService.GetNode(nodeId);
            if (node == null)
            {
                return NotFound();
            }

            return Ok(node.ToDto());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving node {NodeId}", nodeId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("networkitem/{nodeId}")]
    public IActionResult GetNetworkItem(byte nodeId)
    {
        try
        {
            var item = _deviceService.GetNetworkItem(nodeId);
            if (item == null)
            {
                return NotFound();
            }

            return Ok(item.ToDto());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving network item {NodeId}", nodeId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("bynode/{nodeId}/lamp/toggle")]
    public IActionResult ToggleLampByNode(byte nodeId)
    {
        var networkItem = _deviceService.GetNetworkItem(nodeId);
        if (networkItem is not ILamp lamp)
            return NotFound($"No lamp found for node id {nodeId}");
        _ = lamp.ToggleState();
        return Ok();
    }

    [HttpPost("bynode/{nodeId}/lamp/warmwhite")]
    public async Task<IActionResult> SetLampWarmWhiteByNode(byte nodeId)
    {
        var networkItem = _deviceService.GetNetworkItem(nodeId);
        if (networkItem is not ILamp lamp)
            return NotFound($"No lamp found for node id {nodeId}");
        await lamp.SetWarmWhite();
        return Ok();
    }

    [HttpPost("bynode/{nodeId}/lamp/coldwhite")]
    public async Task<IActionResult> SetLampColdWhiteByNode(byte nodeId)
    {
        var networkItem = _deviceService.GetNetworkItem(nodeId);
        if (networkItem is not ILamp lamp)
            return NotFound($"No lamp found for node id {nodeId}");
        await lamp.SetColdWhite();
        return Ok();
    }

    [HttpPost("bynode/{nodeId}/lamp/color/{color}/brightness/{brightness}")]
    public async Task<IActionResult> SetLampColorAndBrightnessByNode(byte nodeId, string color, byte brightness)
    {
        var networkItem = _deviceService.GetNetworkItem(nodeId);
        if (networkItem is not ILamp lamp)
            return NotFound($"No lamp found for node id {nodeId}");
        await lamp.SetBrightness(brightness);
        if (!string.IsNullOrEmpty(color))
            await lamp.SetColor(color);
        return Ok();
    }

    [HttpPost("bynode/{nodeId}/wallplug/state/{isOn}")]
    public async Task<IActionResult> SetWallPlugStateByNode(byte nodeId, bool isOn)
    {
        var networkItem = _deviceService.GetNetworkItem(nodeId);
        if (networkItem is not IWallPlug wp)
            return NotFound($"No wallplug found for node id {nodeId}");
        await wp.SetState(isOn);
        return Ok();
    }

    [HttpPost("bynode/{nodeId}/wallplug/toggle")]
    public IActionResult ToggleWallPlugByNode(byte nodeId)
    {
        var networkItem = _deviceService.GetNetworkItem(nodeId);
        if (networkItem is not IWallPlug wp)
            return NotFound($"No wallplug found for node id {nodeId}");
        wp.ToggleState();
        return Ok();
    }

    [HttpPost("lamp/{id}/brightness/{brightness}")]
    public async Task<IActionResult> SetLampBrightness(long id, byte brightness)
    {
        try
        {
            var device = await _dbContext.Devices.FirstOrDefaultAsync(d => d.Id == id);
            if (device == null)
            {
                return NotFound($"No lamp found for device id {id}");
            }

            await _deviceService.SetLampBrightness(id, brightness);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting brightness for lamp {LampId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("wallplug/{id}/{isOn}")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<IActionResult> SetWallplugState(long id, bool isOn)
    {
        if (id < byte.MinValue || id > byte.MaxValue)
        {
            return BadRequest("Wallplug id must be between 0 and 255");
        }

        try
        {
            var networkItem = _deviceService.GetNetworkItem((byte)id);
            if (networkItem is not IWallPlug wallPlug)
            {
                return NotFound($"No wallplug found for node id {id}");
            }

            await wallPlug.SetState(isOn);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting state {IsOn} for wallplug {WallplugId}", isOn, id);
            return StatusCode(500, "Internal server error");
        }
    }



    [HttpPost("lamp/{id}/warmwhite")]
    public async Task<IActionResult> SetLampWarmWhite(long id)
    {
        try
        {
            var device = await _dbContext.Devices.FirstOrDefaultAsync(d => d.Id == id);
            if (device == null)
            {
                return NotFound($"No lamp found for device id {id}");
            }

            var networkItem = _deviceService.GetNetworkItem(device.NodeID);
            if (networkItem is not ILamp lamp)
            {
                return NotFound($"No lamp found for device id {id}");
            }

            await lamp.SetWarmWhite();
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting warm white for lamp {LampId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("lamp/{id}/coldwhite")]
    public async Task<IActionResult> SetLampColdWhite(long id)
    {
        try
        {
            var device = await _dbContext.Devices.FirstOrDefaultAsync(d => d.Id == id);
            if (device == null)
            {
                return NotFound($"No lamp found for device id {id}");
            }

            var networkItem = _deviceService.GetNetworkItem(device.NodeID);
            if (networkItem is not ILamp lamp)
            {
                return NotFound($"No lamp found for device id {id}");
            }

            await lamp.SetColdWhite();
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cold white for lamp {LampId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("lamp/{id}/color/{color}")]
    public async Task<IActionResult> SetLampColor(long id, string color)
    {
        if (string.IsNullOrWhiteSpace(color))
        {
            return BadRequest("Color is required");
        }

        try
        {
            var device = await _dbContext.Devices.FirstOrDefaultAsync(d => d.Id == id);
            if (device == null)
            {
                return NotFound($"No lamp found for device id {id}");
            }

            var networkItem = _deviceService.GetNetworkItem(device.NodeID);
            if (networkItem is not ILamp lamp)
            {
                return NotFound($"No lamp found for device id {id}");
            }

            await lamp.SetColor(color);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting color for lamp {LampId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("wallplug/{id}/state/{isOn}")]
    public async Task<IActionResult> SetWallplugStateByDeviceId(long id, bool isOn)
    {
        try
        {
            var device = await _dbContext.Devices.FirstOrDefaultAsync(d => d.Id == id);
            if (device == null)
            {
                return NotFound($"No wallplug found for device id {id}");
            }

            var networkItem = _deviceService.GetNetworkItem(device.NodeID);
            if (networkItem is not IWallPlug wallPlug)
            {
                return NotFound($"No wallplug found for device id {id}");
            }

            await wallPlug.SetState(isOn);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting state {IsOn} for wallplug {WallplugId}", isOn, id);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("thermostat/{id}/temperature/{temperature}")]
    public async Task<IActionResult> SetThermostatTemperature(long id, float temperature)
    {
        if (temperature < 4.5f || temperature > 30.0f)
        {
            return BadRequest("Temperature must be between 4.5 and 30.0 °C");
        }

        try
        {
            var device = await _dbContext.Devices.FirstOrDefaultAsync(d => d.Id == id);
            if (device == null)
            {
                return NotFound($"No thermostat found for device id {id}");
            }

            var networkItem = _deviceService.GetNetworkItem(device.NodeID);
            if (networkItem is not IThermoElement thermostat)
            {
                return NotFound($"No thermostat found for device id {id}");
            }

            await thermostat.SetTemperature(temperature);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting temperature {Temperature} for thermostat {ThermostatId}", temperature, id);
            return StatusCode(500, "Internal server error");
        }
    }
    [HttpGet("nodes/{nodeId}/association-groups")]
    public async Task<IActionResult> GetAssociationGroups(byte nodeId)
    {
        try
        {
            var groups = await _deviceService.GetAssociationGroups(nodeId);
            var dto = groups.Select(g => g.ToDto()).ToList();
            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving association groups for node {NodeId}", nodeId);
            return StatusCode(500, "Internal server error");
        }
    }

    public record SetAssociationGroupRequest(byte[] NodeIds);

    [HttpPost("nodes/{nodeId}/association-groups/{groupId}")]
    public async Task<IActionResult> SetAssociationGroup(byte nodeId, byte groupId, [FromBody] SetAssociationGroupRequest request)
    {
        try
        {
            await _deviceService.SetAssociationGroup(nodeId, groupId, request.NodeIds);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting association group {GroupId} for node {NodeId}", groupId, nodeId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("gpstracker/{nodeId}")]
    public async Task<IActionResult> GetGpsTrackerByNodeId(byte nodeId)
    {
        try
        {
            var tracker = await _deviceService.GetGpsTrackerByNodeId(nodeId);
            if (tracker == null)
            {
                return NotFound();
            }

            return Ok(tracker);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving GPS tracker with NodeID {NodeId}", nodeId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("room/{roomId}/summary")]
    public async Task<IActionResult> GetRoomSummary(long roomId, CancellationToken cancellationToken)
    {
        try
        {
            var devicesInRoom = await _dbContext.Devices
                .Where(d => d.Id_Room == roomId)
                .ToListAsync(cancellationToken);

            var temperatures = new List<float>();
            bool anyDoorOpen = false;
            bool anyPresence = false;

            foreach (var device in devicesInRoom)
            {
                var networkItem = _deviceService.GetNetworkItem(device.NodeID);
                if (networkItem == null) continue;

                if (networkItem is ITemperatureSensor tempSensor && tempSensor.Temperature != null)
                {
                    temperatures.Add(tempSensor.Temperature.Value);
                }

                if (networkItem is IDoorSensor doorSensor && doorSensor.State == DoorSensorState.Offen)
                {
                    anyDoorOpen = true;
                }

                if (networkItem is IMultiSensor multiSensor && multiSensor.Presence != null && multiSensor.Presence.Value > 0)
                {
                    anyPresence = true;
                }
            }

            var summary = new RoomSummaryDto
            {
                RoomId = roomId,
                AverageTemperature = CalculateAverageRoomTemperature(temperatures),
                AnyDoorOpen = anyDoorOpen,
                AnyPresence = anyPresence
            };

            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving room summary for room {RoomId}", roomId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Calculates the average room temperature, excluding sensors reporting 0 (offline/uninitialized).
    /// </summary>
    internal static double? CalculateAverageRoomTemperature(IEnumerable<float> readings)
    {
        var valid = readings.Where(t => t > 0).ToList();
        return valid.Count > 0 ? Math.Round(valid.Average(), 1) : null;
    }
}
