using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Sarah.API.Interfaces;
using Sarah.API.Interfaces.Service;
using Microsoft.EntityFrameworkCore;
using Sarah.Server.Models.Dtos;
using Sarah.Data.Models;
using Sarah.API.BusinessObjects;

namespace Sarah.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class RoomsController : ControllerBase
    {
        private readonly IDBService _dbService;
        private readonly ILogger<RoomsController> _logger;

        private string CurrentUserName => User.Identity?.Name ?? "";

        public RoomsController(IDBService dbService, ILogger<RoomsController> logger)
        {
            _dbService = dbService;
            _logger = logger;
        }

        [HttpGet()]
        public async Task<ActionResult<IEnumerable<RoomDto>>> GetRooms()
        {
            _logger.LogInformation("Getting rooms");
            var rooms = await _dbService.Rooms.ToListAsync();
            return Ok(rooms.Select(room => room.ToDto(_dbService.UserFavourites.FirstOrDefault(x => x.ItemId == room.Id && x.UserId == CurrentUserName && x.ItemType == DashboardItemType.Room) != null)));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<RoomDto>> GetRoom(int id)
        {
            _logger.LogInformation("Getting room with ID {RoomId}", id);
            var room = await _dbService.Rooms.FirstOrDefaultAsync(item => item.Id == id);
            if (room == null)
            {
                _logger.LogWarning("Room with ID {RoomId} not found", id);
                return NotFound();
            }
            var userFavouriteEntity = await _dbService.UserFavourites.FirstOrDefaultAsync(x => x.ItemId == room.Id && x.UserId == CurrentUserName && x.ItemType == DashboardItemType.Room);
            return Ok(room.ToDto(userFavouriteEntity != null));
        }

        [HttpPost]
        public async Task<IActionResult> CreateRoom([FromBody] RoomDto roomDto)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for room creation");
                return BadRequest(ModelState);
            }

            var createdRoom = _dbService.Rooms.Add(roomDto.ToEntity());
            await _dbService.SaveChangesAsync();
            _logger.LogInformation("Room created with ID {RoomId}", createdRoom.Entity.Id);
            return CreatedAtAction(nameof(GetRoom), new { id = createdRoom.Entity.Id }, createdRoom.Entity);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateRoom(int id, [FromBody] RoomDto roomDto)
        {
            if (!ModelState.IsValid)
            {
            _logger.LogWarning("Invalid model state for room update");
            return BadRequest(ModelState);
            }

            var room = await _dbService.Rooms.FirstOrDefaultAsync(item => item.Id == id);
            if (room == null)
            {
            _logger.LogWarning("Room with ID {RoomId} not found for update", id);
            return NotFound();
            }

            room.Name = roomDto.Name;
            // Update other properties as needed

            _dbService.Rooms.Update(room);
            await _dbService.SaveChangesAsync();

            _logger.LogInformation("Room with ID {RoomId} updated", id);
            return Ok(room);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRoom(int id)
        {
            var room = await _dbService.Rooms.FirstOrDefaultAsync(item => item.Id == id);
            if (room == null)
            {
                _logger.LogWarning("Room with ID {RoomId} not found for deletion", id);
                return NotFound();
            }

            _dbService.Rooms.Remove(room);
            await _dbService.SaveChangesAsync();

            _logger.LogInformation("Room with ID {RoomId} deleted", id);
            return NoContent();
        }


        [HttpPut("{id}/favourite/{isFavourite}")]
        public async Task<ActionResult> SetFavourite(long id, bool isFavourite)
        {
            // Set a device as favourite
            Room? entity = _dbService.Rooms.Find(id);
            if (entity == null)
            {
                return NotFound("Room not found");
            }

            var existing = await _dbService.UserFavourites.FirstOrDefaultAsync(x => x.ItemId == id && x.UserId == CurrentUserName && x.ItemType == DashboardItemType.Room);

            if (isFavourite)
            {
                if (existing != null)
                {
                    return BadRequest("Room already marked as favourite");
                }
                _dbService.UserFavourites.Add(new UserFavourite
                {
                    ItemId = id,
                    UserId = CurrentUserName,
                    ItemType = DashboardItemType.Room
                });
            }
            else
            {
                if (existing != null)
                {
                    _dbService.UserFavourites.Remove(existing);
                }
            }

            await _dbService.SaveChangesAsync();
            return Ok();
        }
    }
}