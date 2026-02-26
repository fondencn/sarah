using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sarah.API.Interfaces.Services;
using Sarah.API.BusinessObjects.DTOs;
using Sarah.Persons.WebApi.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Sarah.Persons.WebApi.Data;

namespace Sarah.Persons.WebApi.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class PersonsController : ControllerBase
{
    private readonly IPersonService _personService;
    private readonly ApplicationDbContext _database;
    private readonly ILogger<PersonsController> _logger;

    public PersonsController(IPersonService personService, ApplicationDbContext database, ILogger<PersonsController> logger)
    {
        _personService = personService;
        _database = database;
        _logger = logger;
    }

    private static object MapToDto(API.Interfaces.IPerson p) => new
    {
        id = p.Id,
        name = p.Name,
        gpsTrackerID = p.GPSTrackerID,
        gpsTrackerName = p.TrackerDeviceName,
        currentGeoFence = p.CurrentGeoFence?.Name,
        currentPosition = (string?)null,
        isAtHome = p.IsAtHome,
        mobilePhoneHostname = p.MobilePhoneHostname,
        isFavourite = (p as PersonInfoEntity)?.IsFavourite ?? false
    };

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var persons = await _personService.GetAllPersonsAsync();
            return Ok(persons.Select(MapToDto));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving persons");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(long id)
    {
        try
        {
            var person = await _personService.GetPersonByIdAsync(id);
            if (person == null)
            {
                return NotFound();
            }
            return Ok(MapToDto(person));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving person with id {PersonId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] PersonDto personDto)
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
            return Ok(MapToDto(entity));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating person");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(long id, [FromBody] PersonDto personDto)
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
            return Ok(MapToDto(updated));
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
