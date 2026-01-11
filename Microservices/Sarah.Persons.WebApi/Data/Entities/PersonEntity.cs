using System.ComponentModel.DataAnnotations;

namespace Sarah.Persons.WebApi.Data.Entities;

public class PersonEntity
{
    public Guid Id { get; set; }
    [Required][MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    [MaxLength(200)]
    public string? Email { get; set; }
    [MaxLength(100)]
    public string? MacAddress { get; set; }
    public bool IsPresent { get; set; }
    public DateTime? LastSeen { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
