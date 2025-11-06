using Chat.Domain.Aggregates;
using Chat.Domain.Repositories;
using EMIS.BuildingBlocks.ApiResponse;
using EMIS.BuildingBlocks.Exceptions;
using MediatR;

namespace Chat.Application.Commands.Conversations;

/// <summary>
/// Handler for creating OneToOne conversation
/// Business Logic: Check if conversation already exists between two users
/// </summary>
public class CreateOneToOneConversationCommandHandler 
    : IRequestHandler<CreateOneToOneConversationCommand, ApiResponse<Guid>>
{
    private readonly IConversationRepository _conversationRepository;

    public CreateOneToOneConversationCommandHandler(
        IConversationRepository conversationRepository)
    {
        _conversationRepository = conversationRepository;
    }

    public async Task<ApiResponse<Guid>> Handle(
        CreateOneToOneConversationCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Check if conversation already exists
            var existingConversation = await _conversationRepository.FindOneToOneConversationAsync(
                request.User1Id,
                request.User2Id,
                request.TenantId,
                cancellationToken);

            if (existingConversation != null)
            {
                // Return existing conversation ID
                return ApiResponse<Guid>.SuccessResult(existingConversation.ConversationId);
            }

            // Create new conversation
            var conversation = Conversation.CreateOneToOne(
                request.TenantId,
                request.User1Id,
                request.User1Name,
                request.User2Id,
                request.User2Name);

            await _conversationRepository.AddAsync(conversation, cancellationToken);

            return ApiResponse<Guid>.SuccessResult(conversation.ConversationId);
        }
        catch (BusinessRuleValidationException ex)
        {
            return ApiResponse<Guid>.ErrorResult(ex.Message, 400);
        }
        catch (Exception ex)
        {
            return ApiResponse<Guid>.ErrorResult($"Failed to create conversation: {ex.Message}", 500);
        }
    }
}
