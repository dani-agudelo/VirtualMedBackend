using MediatR;
using VirtualMed.Application.Chatbot;

namespace VirtualMed.Application.Queries.Chatbot;

public record GetMyChatConversationQuery : IRequest<ChatConversationDto>;
