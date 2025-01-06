using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Sarah.API.BusinessObjects;

namespace Sarah.Data.Models
{
    [Table("Rules")]
    public class RuleInfo
    {
        [Column]
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        [NotMapped]
        public Rule? Rule { get; set; }

        [Column]
        public byte[]? RuleSerialized
        {
            get => this.Rule?.Serialize();
            set => this.Rule = Rule.Deserialize(value);
        }


        public string RuleName => Rule?.Name ?? String.Empty;


    }
}
