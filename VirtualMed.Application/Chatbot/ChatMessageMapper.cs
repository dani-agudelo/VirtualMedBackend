using System.Text.Json;
using VirtualMed.Application.Interfaces.Services;
using VirtualMed.Domain.Enums;

namespace VirtualMed.Application.Chatbot;

internal static class ChatMessageMapper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public static ChatMessageDto ToDto(Domain.Entities.ChatMessage message)
    {
        IReadOnlyList<ChatSourceDto>? sources = null;
        if (!string.IsNullOrWhiteSpace(message.SourcesJson))
        {
            sources = JsonSerializer.Deserialize<List<ChatSourceDto>>(message.SourcesJson, JsonOptions)
                      ?? [];
        }

        return new ChatMessageDto
        {
            Id = message.Id,
            Role = message.Role.ToString(),
            Content = message.Content,
            Sources = sources,
            CreatedAt = message.CreatedAt
        };
    }

    public static ChatMessageDto ToDto(
        Domain.Entities.ChatMessage message,
        IReadOnlyList<ChatbotSourceItem> sources)
    {
        return new ChatMessageDto
        {
            Id = message.Id,
            Role = message.Role.ToString(),
            Content = message.Content,
            Sources = sources.Select(s => new ChatSourceDto
            {
                FileName = s.FileName,
                PageLabel = s.PageLabel,
                Score = s.Score
            }).ToList(),
            CreatedAt = message.CreatedAt
        };
    }

    public static string? SerializeSources(IReadOnlyList<ChatbotSourceItem> sources)
    {
        if (sources.Count == 0)
            return null;

        var payload = sources.Select(s => new ChatSourceDto
        {
            FileName = s.FileName,
            PageLabel = s.PageLabel,
            Score = s.Score
        }).ToList();

        return JsonSerializer.Serialize(payload, JsonOptions);
    }
}
