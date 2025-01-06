using Sarah.API.BusinessObjects;
using Sarah.API.Interfaces;
using Sarah.API.Interfaces.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace Sarah.Data.Models
{
    [Table("Devices")]
    public class DeviceInfo
    {
        [Column]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key]
        public long Id { get; set; }

        [Column]
        public long? Id_Room { get; set; }

        [Display(Name = "Gerätename")]
        [Column]
        public string? Name { get; set; }

        [Display(Name = "Z-Wave Node ID")]
        [Column]
        public byte NodeID { get; set; }

        [Display(Name = "Geräteklasse")]
        [Column(TypeName = "int")]
        [EnumDataType(typeof(KnownDeviceTypes))]
        public KnownDeviceTypes SpecificType {get;set; }


        [Display(Name = "Schreibgeschützt")]
        [Column]
        public bool IsReadonly { get; set; }

        public INetworkElement GetNetworkItem(IDeviceService deviceService)
        {
            return deviceService.GetNetworkItem(NodeID);
        }
    }
}
