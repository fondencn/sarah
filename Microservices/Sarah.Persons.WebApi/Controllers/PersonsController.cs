using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sarah.API.Interfaces.Services;
using Sarah.API.BusinessObjects.DTOs;
using Sarah.Persons.WebApi.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Sarah.Persons.WebApi.Data;
using Sarah.Persons.WebApi.DTOs;
using Sarah.Persons.WebApi.Services;
using Sarah.API.BusinessObjects;

namespace Sarah.Persons.WebApi.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class PersonsController : ControllerBase
{
    private readonly IPersonService _personService;
    private readonly INamedPositionService _namedPositionService;
    private readonly ApplicationDbContext _database;
    private readonly ILogger<PersonsController> _logger;

    public PersonsController(IPersonService personService, INamedPositionService namedPositionService, ApplicationDbContext database, ILogger<PersonsController> logger)
    {
        _personService = personService;
        _namedPositionService = namedPositionService;
        _database = database;
        _logger = logger;
    }

    private async Task<PersonResponseDto> MapToDto(API.Interfaces.IPerson p) => new PersonResponseDto
    {
        Id = p.Id,
        Name = p.Name,
        GpsTrackerID = p.GPSTrackerID,
        GpsTrackerName = p.TrackerDeviceName,
        CurrentGeoFence = p.CurrentGeoFence?.Name,
        CurrentPositionLong = p.GPSTracker?.Position?.Longtitude.Value,
        CurrentPositionLat = p.GPSTracker?.Position?.Latitude.Value,
        CurrentNamedPosition = await GetCurrentNamedPosition(p.GPSTracker?.Position),
        IsAtHome = p.IsAtHome,
        MobilePhoneHostname = p.MobilePhoneHostname,
        IsFavourite = (p as PersonInfoEntity)?.IsFavourite ?? false
    };

    [NonAction]
    public async Task<string> GetCurrentNamedPosition(LocatorPosition? position)
    {
        return await _namedPositionService.ResolveNamedPositionAsync(position, HttpContext.RequestAborted);
    }

    [HttpGet("namedposition")]
    public async Task<ActionResult<string>> GetNamedPosition([FromQuery] float? lat, [FromQuery] float? lon)
    {
        if (!lat.HasValue || !lon.HasValue)
        {
            return BadRequest("Both lat and lon query parameters are required.");
        }

        var position = new LocatorPosition(new Sarah.API.Business.SensorData(lat.Value, "°"), new Sarah.API.Business.SensorData(lon.Value, "°"));
        var namedPosition = await GetCurrentNamedPosition(position);
        return Ok(namedPosition);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PersonResponseDto>>> GetAll()
    {
        try
        {
            var persons = await _personService.GetAllPersonsAsync();
            var personDtos = new List<PersonResponseDto>();
            foreach (var person in persons)
            {
                personDtos.Add(await MapToDto(person));
            }
            return Ok(personDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving persons");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<PersonResponseDto>> GetById(long id)
    {
        try
        {
            var person = await _personService.GetPersonByIdAsync(id);
            if (person == null)
            {
                return NotFound();
            }
            return Ok(await MapToDto(person));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving person with id {PersonId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost]
    public async Task<ActionResult<PersonResponseDto>> Create([FromBody] PersonDto personDto)
    {
        try
        {
            if (personDto == null)
            {
                return BadRequest();
            }
            var entity = new PersonInfoEntity
            {
                Name = personDto.Name,
                MobilePhoneHostname = personDto.MobilePhoneHostname,
                GPSTrackerID = personDto.GPSTrackerID
            };
            await _database.Persons.AddAsync(entity);
            await _database.SaveChangesAsync();
            return Ok(await MapToDto(entity));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating person");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<PersonResponseDto>> Update(long id, [FromBody] PersonDto personDto)
    {
        try
        {
            if (personDto == null)
            {
                return BadRequest();
            }
            personDto.Id = id;
            await _personService.UpdatePersonAsync(personDto);
            var updated = await _personService.GetPersonByIdAsync(id);
            if (updated == null) return NotFound();
            return Ok(await MapToDto(updated));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating person with id {PersonId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(long id)
    {
        try
        {
            await _personService.DeletePersonAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting person with id {PersonId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPut("{id}/favourite/{isFavourite}")]
    public async Task<IActionResult> SetFavourite(long id, bool isFavourite)
    {
        try
        {
            var entity = await _database.Persons.FirstOrDefaultAsync(p => p.Id == id);
            if (entity == null)
            {
                return NotFound();
            }
            entity.IsFavourite = isFavourite;
            await _database.SaveChangesAsync();
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting favourite for person {PersonId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("known-home-network-devices")]
    public async Task<IActionResult> GetKnownHomeNetworkDevices()
    {
        try
        {
            var devices = await _personService.GetKnownHomeNetworkDevices();
            return Ok(devices);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving known home network devices");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("status")]
    [AllowAnonymous]
    public IActionResult GetStatus()
    {
        return Ok(new { status = "running", service = "PersonService" });
    }
}
