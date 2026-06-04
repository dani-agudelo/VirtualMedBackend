using MediatR;
using Microsoft.EntityFrameworkCore;
using VirtualMed.Application.Chatbot;
using VirtualMed.Application.Interfaces;
using VirtualMed.Domain.Entities;

namespace VirtualMed.Application.Queries.Chatbot;

public class GetMyChatConversationQueryHandler : IRequestHandler<GetMyChatConversationQuery, ChatConversationDto>
{
    private const int MaxMessages = 200;

    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetMyChatConversationQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<ChatConversationDto> Handle(
        GetMyChatConversationQuery request,
        CancellationToken cancellationToken)
    {
        var patientId = await PatientChatbotAccessResolver.ResolveSelfPatientIdAsync(
            _context,
            _currentUser,
            cancellationToken);

        var conversation = await _context.Set<ChatConversation>()
            .FirstOrDefaultAsync(c => c.PatientId == patientId, cancellationToken);

        if (conversation is null)
        {
            var now = DateTime.UtcNow;
            conversation = new ChatConversation
            {
                Id = Guid.NewGuid(),
                PatientId = patientId,
                CreatedAt = now,
                UpdatedAt = now
            };
            _context.Add(conversation);
            await _context.SaveChangesAsync(cancellationToken);

            return new ChatConversationDto
            {
                Id = conversation.Id,
                PatientId = conversation.PatientId,
                CreatedAt = conversation.CreatedAt,
                UpdatedAt = conversation.UpdatedAt,
                Messages = []
            };
        }

        var messages = await _context.Set<ChatMessage>()
            .AsNoTracking()
            .Where(m => m.ConversationId == conversation.Id)
            .OrderBy(m => m.CreatedAt)
            .Take(MaxMessages)
            .ToListAsync(cancellationToken);

        return new ChatConversationDto
        {
            Id = conversation.Id,
            PatientId = conversation.PatientId,
            CreatedAt = conversation.CreatedAt,
            UpdatedAt = conversation.UpdatedAt,
            Messages = messages.Select(ChatMessageMapper.ToDto).ToList()
        };
    }
}
