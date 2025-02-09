using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

        public PersonsController(IPersonService personService)
        {
            _personService = personService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<PersonDto>>> GetAllPersons()
        {
            var persons = await _personService.GetAllPersonsAsync();
            var personDtos = persons.Select(person => person.ToDto());
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
            var personDto = person.ToDto();
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
    }
}