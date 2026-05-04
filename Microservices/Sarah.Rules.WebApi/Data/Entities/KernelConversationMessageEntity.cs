using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sarah.Rules.WebApi.Data.Entities;

[Table("KernelConversationMessages")]
public class KernelConversationMessageEntity
{
    [Key]
    public long Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string ConversationId { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string Role { get; set; } = string.Empty;

    [Required]
    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }
}