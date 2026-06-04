using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using VirtualMed.Application.Chatbot;
using VirtualMed.Application.Common.Exceptions;
using VirtualMed.Application.Configuration;
using VirtualMed.Application.Interfaces;
using VirtualMed.Application.Interfaces.Services;
using VirtualMed.Domain.Entities;
using VirtualMed.Domain.Enums;

namespace VirtualMed.Application.Commands.Chatbot;

public class SendChatMessageCommandHandler : IRequestHandler<SendChatMessageCommand, SendChatMessageResultDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IChatbotClient _chatbotClient;
    private readonly ChatbotSettings _settings;

    public SendChatMessageCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IChatbotClient chatbotClient,
        IOptions<ChatbotSettings> settings)
    {
        _context = context;
        _currentUser = currentUser;
        _chatbotClient = chatbotClient;
        _settings = settings.Value;
    }

    public async Task<SendChatMessageResultDto> Handle(
        SendChatMessageCommand request,
        CancellationToken cancellationToken)
    {
        var patientId = await PatientChatbotAccessResolver.ResolveSelfPatientIdAsync(
            _context,
            _currentUser,
            cancellationToken);

        var conversation = await _context.Set<ChatConversation>()
            .FirstOrDefaultAsync(c => c.PatientId == patientId, cancellationToken);

        var now = DateTime.UtcNow;
        if (conversation is null)
        {
            conversation = new ChatConversation
            {
                Id = Guid.NewGuid(),
                PatientId = patientId,
                CreatedAt = now,
                UpdatedAt = now
            };
            _context.Add(conversation);
            await _context.SaveChangesAsync(cancellationToken);
        }

        var userMessage = new ChatMessage
        {
            Id = Guid.NewGuid(),
            ConversationId = conversation.Id,
            Role = ChatMessageRole.User,
            Content = request.Message.Trim(),
            CreatedAt = now
        };
        _context.Add(userMessage);
        conversation.UpdatedAt = now;
        await _context.SaveChangesAsync(cancellationToken);

        var health = await _chatbotClient.GetHealthAsync(cancellationToken);
        if (!string.Equals(health.Status, "ok", StringComparison.OrdinalIgnoreCase))
            throw new ExternalServiceException(
                "El asistente clínico no está disponible en este momento.",
                "Chatbot");

        var chatbotResponse = await _chatbotClient.SendMessageAsync(
            conversation.Id.ToString(),
            userMessage.Content,
            _settings.SimilarityTopK,
            cancellationToken);

        var assistantNow = DateTime.UtcNow;
        var assistantMessage = new ChatMessage
        {
            Id = Guid.NewGuid(),
            ConversationId = conversation.Id,
            Role = ChatMessageRole.Assistant,
            Content = chatbotResponse.Answer,
            SourcesJson = ChatMessageMapper.SerializeSources(chatbotResponse.Sources),
            CreatedAt = assistantNow
        };
        _context.Add(assistantMessage);
        conversation.UpdatedAt = assistantNow;
        await _context.SaveChangesAsync(cancellationToken);

        return new SendChatMessageResultDto
        {
            UserMessage = ChatMessageMapper.ToDto(userMessage),
            AssistantMessage = ChatMessageMapper.ToDto(assistantMessage, chatbotResponse.Sources)
        };
    }
}
