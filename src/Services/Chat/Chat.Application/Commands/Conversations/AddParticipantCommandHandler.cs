using Chat.Domain.Enums;
using Chat.Domain.Repositories;
using EMIS.BuildingBlocks.ApiResponse;
using MediatR;

namespace Chat.Application.Commands.Conversations;

/// <summary>
/// Handler for adding participants to conversations
/// </summary>
public class AddParticipantCommandHandler 
    : IRequestHandler<AddParticipantCommand, ApiResponse<bool>>
{
    private readonly IConversationRepository _conversationRepository;

    public AddParticipantCommandHandler(IConversationRepository conversationRepository)
    {
        _conversationRepository = conversationRepository;
    }

    public async Task<ApiResponse<bool>> Handle(
        AddParticipantCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            // 1. Get conversation
            var conversation = await _conversationRepository.GetByIdAsync(
                request.ConversationId,
                cancellationToken);

            if (conversation == null)
                return ApiResponse<bool>.ErrorResult("Conversation not found", 404);

            // 2. Check if requester is admin
            var requesterParticipant = conversation.Participants
                .FirstOrDefault(p => p.UserId == request.RequestedBy);

            if (requesterParticipant == null || requesterParticipant.Role != ParticipantRole.Admin)
                return ApiResponse<bool>.ErrorResult(
                    "Only admins can add participants", 
                    403);

            // 3. Check if user is already a participant
            if (conversation.IsParticipant(request.UserIdToAdd))
                return ApiResponse<bool>.ErrorResult(
                    "User is already a participant", 
                    409);

            // 4. OneToOne conversations cannot have participants added
            if (conversation.Type == ConversationType.OneToOne)
                return ApiResponse<bool>.ErrorResult(
                    "Cannot add participants to one-to-one conversations", 
                    400);

            // 5. Add participant (default to Member role)
            conversation.AddParticipant(
                request.UserIdToAdd, 
                request.UserName, 
                ParticipantRole.Member);

            // 6. Save conversation
            await _conversationRepository.UpdateAsync(conversation, cancellationToken);

            return ApiResponse<bool>.SuccessResult(true);
        }
        catch (Exception ex)
        {
            return ApiResponse<bool>.ErrorResult(
                $"Failed to add participant: {ex.Message}", 
                500);
        }
    }
}
