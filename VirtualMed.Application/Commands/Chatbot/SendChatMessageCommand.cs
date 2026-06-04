using MediatR;
using VirtualMed.Application.Chatbot;

namespace VirtualMed.Application.Commands.Chatbot;

public record SendChatMessageCommand(string Message) : IRequest<SendChatMessageResultDto>;
