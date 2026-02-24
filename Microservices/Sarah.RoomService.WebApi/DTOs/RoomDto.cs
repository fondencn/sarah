namespace Sarah.RoomService.WebApi.DTOs;

public class RoomDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsFavourite { get; set; }
}
