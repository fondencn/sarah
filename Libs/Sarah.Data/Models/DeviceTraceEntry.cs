using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace Sarah.Data.Models
{
    /// <summary>
    /// Ein Datenbankeintrag für ein Ereignis, welches ein Gerät ausgelöst hat
    /// </summary>
    [Table("DeviceTraces")]
    public class DeviceTraceEntry
    {
        /// <summary>
        /// ID
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>
        /// Zwave Node ID des Gerätes
        /// </summary>
        public byte NodeId { get; set; }

        /// <summary>
        /// Zeitpunkt des Auftretens
        /// </summary>
        public DateTime CreationDate { get; set; }

        /// <summary>
        /// Logtext
        /// </summary>
        public string? Message { get; set; }

        /// <summary>
        /// Name der geänderten Eigenschaft
        /// </summary>
        public string? Property { get; set; }

        /// <summary>
        /// Neuer Wert der geänderten Eigenschaft (numerisch)
        /// </summary>
        public string? Value { get; set; }
    }
}
