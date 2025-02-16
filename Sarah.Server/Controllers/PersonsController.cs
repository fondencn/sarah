using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sarah.API.BusinessObjects;
using Sarah.API.Interfaces.Service;
using Sarah.API.Interfaces.Services;
using Sarah.Data.Models;
using Sarah.Server.Models.Dtos;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sarah.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PersonsController : ControllerBase
    {
        private readonly IPersonService _personService;
        private readonly IDBService _databaseService;

        private string CurrentUserName => User.Identity?.Name ?? "";

        public PersonsController(IPersonService personService, IDBService databaseService)
        {
            _personService = personService;
            _databaseService = databaseService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<PersonDto>>> GetAllPersons()
        {
            var persons = await _personService.GetAllPersonsAsync();
            var personDtos = persons.Select(person => person.ToDto(_databaseService.UserFavourites.FirstOrDefault(x => x.ItemId == person.Id && x.UserId == CurrentUserName && x.ItemType == DashboardItemType.Person) != null));
            return Ok(personDtos);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<PersonDto>> GetPersonById(int id)
        {
            var person = await _personService.GetPersonByIdAsync(id);
            if (person == null)
            {
                return NotFound();
            }

            var favouriteEntity = await _databaseService.UserFavourites.FirstOrDefaultAsync(x => x.ItemId == id && x.UserId == CurrentUserName && x.ItemType == DashboardItemType.Person);
            var personDto = person.ToDto(favouriteEntity != null);
            return Ok(personDto);
        }

        [HttpPost]
        public async Task<ActionResult> AddPerson(PersonDto personDto)
        {
            var person = personDto.ToEntity();
            await _personService.AddPersonAsync(person);
            return CreatedAtAction(nameof(GetPersonById), new { id = person.Id }, personDto);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> UpdatePerson(int id, PersonDto personDto)
        {
            if (id != personDto.Id)
            {
                return BadRequest();
            }
            var person = personDto.ToEntity();
            await _personService.UpdatePersonAsync(person);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeletePerson(int id)
        {
            await _personService.DeletePersonAsync(id);
            return NoContent();
        }



        [HttpPut("{id}/favourite/{isFavourite}")]
        public async Task<ActionResult> SetFavourite(long id, bool isFavourite)
        {
            // Set a device as favourite
            PersonInfo? entity = _databaseService.Persons.Find(id);
            if (entity == null)
            {
                return NotFound("Room not found");
            }

            var existing = await _databaseService.UserFavourites.FirstOrDefaultAsync(x => x.ItemId == id && x.UserId == CurrentUserName && x.ItemType == DashboardItemType.Person);

            if (isFavourite)
            {
                if (existing != null)
                {
                    return BadRequest("Person already marked as favourite");
                }
                _databaseService.UserFavourites.Add(new UserFavourite
                {
                    ItemId = id,
                    UserId = CurrentUserName,
                    ItemType = DashboardItemType.Person
                });
            }
            else
            {
                if (existing != null)
                {
                    _databaseService.UserFavourites.Remove(existing);
                }
            }

            await _databaseService.SaveChangesAsync();
            return Ok();
        }
    }
}