using VirtualMed.Domain.Enums;

namespace VirtualMed.Domain.Entities;

public class ChatMessage
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public ChatConversation Conversation { get; set; } = null!;
    public ChatMessageRole Role { get; set; }
    public string Content { get; set; } = null!;
    public string? SourcesJson { get; set; }
    public DateTime CreatedAt { get; set; }
}
