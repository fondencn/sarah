using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Sarah.API.BusinessObjects;

namespace Sarah.Persons.WebApi.Data.Entities;

[Table("UserFavourites")]
public class UserFavouriteEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column]
    public int Id { get; set; }
    
    [Column]
    public string UserId { get; set; } = "";
    
    [Column]
    public long ItemId { get; set; }
    
    [Column(TypeName = "int")]
    [EnumDataType(typeof(DashboardItemType))]
    public DashboardItemType ItemType { get; set; }
}
