using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sarah.RoomService.WebApi.Data.Entities;

[Table("Rooms")]
public class RoomEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column]
    public long Id { get; set; }

    [Required]
    [MaxLength(200)]
    [Display(Name = "Raumbezeichnung")]
    [Column]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    [Display(Name = "Beschreibung")]
    [Column]
    public string? Description { get; set; }

    [Display(Name = "Erstellt am")]
    [Column]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Display(Name = "Aktualisiert am")]
    [Column]
    public DateTime? UpdatedAt { get; set; }

    [Display(Name = "Favorit")]
    [Column]
    public bool IsFavourite { get; set; }
}
