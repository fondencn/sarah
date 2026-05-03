using System;

namespace Sarah.API.BusinessObjects.DTOs
{
    public class KernelConversationMessageDto
    {
        public long Id { get; set; }
        public string ConversationId { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAtUtc { get; set; }
    }
}