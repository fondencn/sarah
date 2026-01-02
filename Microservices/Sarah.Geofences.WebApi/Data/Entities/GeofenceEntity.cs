using System.ComponentModel.DataAnnotations;

namespace Sarah.Geofences.WebApi.Data.Entities;

public class GeofenceEntity
{
    public Guid Id { get; set; }
    [Required][MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double Radius { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
