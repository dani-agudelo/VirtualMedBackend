namespace VirtualMed.Application.Chatbot;

public sealed class ChatConversationDto
{
    public Guid Id { get; init; }
    public Guid PatientId { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public IReadOnlyList<ChatMessageDto> Messages { get; init; } = [];
}

public sealed class ChatMessageDto
{
    public Guid Id { get; init; }
    public string Role { get; init; } = null!;
    public string Content { get; init; } = null!;
    public IReadOnlyList<ChatSourceDto>? Sources { get; init; }
    public DateTime CreatedAt { get; init; }
}

public sealed class ChatSourceDto
{
    public string FileName { get; init; } = null!;
    public string PageLabel { get; init; } = null!;
    public double? Score { get; init; }
}

public sealed class SendChatMessageResultDto
{
    public ChatMessageDto UserMessage { get; init; } = null!;
    public ChatMessageDto AssistantMessage { get; init; } = null!;
}
