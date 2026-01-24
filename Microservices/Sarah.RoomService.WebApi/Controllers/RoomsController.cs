using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sarah.RoomService.WebApi.Data.Entities;
using Sarah.RoomService.WebApi.Data.Repositories;
using Sarah.RoomService.WebApi.DTOs;

namespace Sarah.RoomService.WebApi.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class RoomsController : ControllerBase
{
    private readonly IRepository<RoomEntity> _repository;
    private readonly ILogger<RoomsController> _logger;

    public RoomsController(IRepository<RoomEntity> repository, ILogger<RoomsController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <summary>
    /// Get all rooms
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<RoomDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        try
        {
            var rooms = await _repository.GetAllAsync(cancellationToken);
            var roomDtos = rooms.Select(r => new RoomDto
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt
            });
            return Ok(roomDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving rooms");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get a room by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(RoomDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
    {
        try
        {
            var room = await _repository.GetByIdAsync(id, cancellationToken);
            if (room == null)
            {
                return NotFound($"Room with ID {id} not found");
            }

            var roomDto = new RoomDto
            {
                Id = room.Id,
                Name = room.Name,
                Description = room.Description,
                CreatedAt = room.CreatedAt,
                UpdatedAt = room.UpdatedAt
            };
            return Ok(roomDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving room with ID {RoomId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Create a new room
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(RoomDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Create([FromBody] CreateRoomDto createRoomDto, CancellationToken cancellationToken)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var room = new RoomEntity
            {
                Name = createRoomDto.Name,
                Description = createRoomDto.Description,
                CreatedAt = DateTime.UtcNow
            };

            var createdRoom = await _repository.AddAsync(room, cancellationToken);

            var roomDto = new RoomDto
            {
                Id = createdRoom.Id,
                Name = createdRoom.Name,
                Description = createdRoom.Description,
                CreatedAt = createdRoom.CreatedAt,
                UpdatedAt = createdRoom.UpdatedAt
            };

            return CreatedAtAction(nameof(GetById), new { id = roomDto.Id }, roomDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating room");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Update an existing room
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(RoomDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateRoomDto updateRoomDto, CancellationToken cancellationToken)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var room = await _repository.GetByIdAsync(id, cancellationToken);
            if (room == null)
            {
                return NotFound($"Room with ID {id} not found");
            }

            room.Name = updateRoomDto.Name;
            room.Description = updateRoomDto.Description;
            room.UpdatedAt = DateTime.UtcNow;

            await _repository.UpdateAsync(room, cancellationToken);

            var roomDto = new RoomDto
            {
                Id = room.Id,
                Name = room.Name,
                Description = room.Description,
                CreatedAt = room.CreatedAt,
                UpdatedAt = room.UpdatedAt
            };

            return Ok(roomDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating room with ID {RoomId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Delete a room
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
    {
        try
        {
            var room = await _repository.GetByIdAsync(id, cancellationToken);
            if (room == null)
            {
                return NotFound($"Room with ID {id} not found");
            }

            await _repository.DeleteAsync(room, cancellationToken);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting room with ID {RoomId}", id);
            return StatusCode(500, "Internal server error");
        }
    }
}
