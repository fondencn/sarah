using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace Sarah.Data.Models
{
    /// <summary>
    /// Ein Datenbankeintrag für die historisierung von Coronadaten
    /// </summary>
    [Table("DeseaseKpis")]
    public class DeseaseKpi
    {
        /// <summary>
        /// ID
        /// </summary>
        [Column]
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Column]
        public string? DeseaseName { get; set; }
        [Column]
        public string? FieldName { get; set; }
        [Column]
        public double FieldValue { get; set; }
        [Column]
        public DateTime Date { get; set; }

    }
}
