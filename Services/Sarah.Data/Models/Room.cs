using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace Sarah.Data.Models
{
    [Table("Rooms")]
    public class Room
    {
        public static Room Default { get; } = new Room() { Id = 0, Name = "" };

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        [Display(Name = "Raumbezeichnung")]
        public string? Name { get; set; }
    }
}
