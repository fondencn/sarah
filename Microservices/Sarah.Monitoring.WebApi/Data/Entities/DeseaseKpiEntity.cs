using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sarah.Monitoring.WebApi.Data.Entities;

/// <summary>
/// A database entry for historizing disease data
/// </summary>
[Table("DeseaseKpis")]
public class DeseaseKpiEntity
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
