using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sarah.API.BusinessObjects;
using Sarah.API.Interfaces.Service;
using Sarah.API.Interfaces.Services;
using Sarah.Data.Models;
using Sarah.Geofences;
using Sarah.Server.Models.Dtos;

namespace Sarah.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class LocationController(IDeviceService _deviceService, IDBService _databaseService) : ControllerBase
    {
        
        [HttpGet("tracker/{id}")]
        public async Task<ActionResult<LocationDto>> GetGpsTrackerLocation(long id) 
        {
            // Get the location of a GPS tracker
            DeviceInfo? entity = await _databaseService.Devices.FindAsync(id);
            if (entity == null)
            {
                return NotFound($"Device {id} not found");
            }
            var gpsTracker = _deviceService.GPSTrackers.FirstOrDefault(x => x.NodeID == entity.NodeID);
            if (gpsTracker == null)
            {
                return NotFound($"Device {id} is not a GPS tracker");
            }
            return Ok(new LocationDto
            {
                Latitude = gpsTracker.Position.Latitude.Value,
                Longitude = gpsTracker.Position.Longtitude.Value
            });
        }

        [HttpGet("tracker")]
        public ActionResult<IEnumerable<NamedLocationDto>> GetGpsTrackerLocations()
        {
            // Get the location of all GPS trackers
            List<NamedLocationDto> dtos = new List<NamedLocationDto>();
            foreach (var gpsTracker in _deviceService.GPSTrackers)
            {
                var gpsTrackerInfo = _databaseService.Devices.FirstOrDefault(x => x.NodeID == gpsTracker.NodeID);
                if (gpsTrackerInfo == null)
                {
                    return NotFound($"Device {gpsTracker.NodeID} not found");
                }
                dtos.Add(new NamedLocationDto
                {
                    Latitude = gpsTracker.Position.Latitude.Value,
                    Longitude = gpsTracker.Position.Longtitude.Value,
                    Name = gpsTrackerInfo.Name ?? ""
                });
            }
            return Ok(dtos);
        }

        [HttpGet("wellknownlocations")]
        public ActionResult<IEnumerable<NamedLocationDto>> GetWellKnownLocations()
        {
            return Ok(new NamedLocationDto
            {
                Latitude = LocatorPosition.ZuHause.Latitude.Value,
                Longitude = LocatorPosition.ZuHause.Longtitude.Value,
                Name = "Zuhause"
            });
        }

        [HttpGet("geofences")]
        public ActionResult<IEnumerable<GeofenceDto>> GetGeofences()
        {
            return Ok(GeoFenceService.All.Select(x => new GeofenceDto
            {
                Id = 0, // exists not yet in the database
                Name = x.Name,
                Points = x.Points.Select(y => new PointDto
                {
                    Latitude = y.Latitude.Value,
                    Longitude = y.Longtitude.Value
                }).ToList()
            }));
        }
    }
}