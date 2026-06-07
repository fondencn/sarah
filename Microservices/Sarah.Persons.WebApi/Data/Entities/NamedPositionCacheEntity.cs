using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Sarah.Persons.WebApi.Data.Entities;

[Table("NamedPositionCache")]
[Index(nameof(Latitude), nameof(Longitude), IsUnique = true)]
public class NamedPositionCacheEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column]
    public long Id { get; set; }

    [Precision(9, 5)]
    [Column]
    public decimal Latitude { get; set; }

    [Precision(9, 5)]
    [Column]
    public decimal Longitude { get; set; }

    [MaxLength(512)]
    [Column]
    public string NamedLocation { get; set; } = string.Empty;

    [MaxLength(512)]
    [Column]
    public string? DisplayName { get; set; }

    [Column(TypeName = "text")]
    public string? NominatimJson { get; set; }

    [Column]
    public DateTime CreatedAtUtc { get; set; }

    [Column]
    public DateTime LastUsedAtUtc { get; set; }
}
