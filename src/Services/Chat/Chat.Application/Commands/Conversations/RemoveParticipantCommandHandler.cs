using Chat.Domain.Enums;
using Chat.Domain.Repositories;
using EMIS.BuildingBlocks.ApiResponse;
using MediatR;

namespace Chat.Application.Commands.Conversations;

/// <summary>
/// Handler for removing participants from conversations
/// </summary>
public class RemoveParticipantCommandHandler 
    : IRequestHandler<RemoveParticipantCommand, ApiResponse<bool>>
{
    private readonly IConversationRepository _conversationRepository;

    public RemoveParticipantCommandHandler(IConversationRepository conversationRepository)
    {
        _conversationRepository = conversationRepository;
    }

    public async Task<ApiResponse<bool>> Handle(
        RemoveParticipantCommand request,
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
                    "Only admins can remove participants", 
                    403);

            // 3. Check if user to remove is a participant
            if (!conversation.IsParticipant(request.UserIdToRemove))
                return ApiResponse<bool>.ErrorResult(
                    "User is not a participant", 
                    404);

            // 4. OneToOne conversations cannot have participants removed
            if (conversation.Type == ConversationType.OneToOne)
                return ApiResponse<bool>.ErrorResult(
                    "Cannot remove participants from one-to-one conversations", 
                    400);

            // 5. Cannot remove yourself if you're the last admin
            if (request.RequestedBy == request.UserIdToRemove)
            {
                var adminCount = conversation.Participants
                    .Count(p => p.Role == ParticipantRole.Admin);
                
                if (adminCount == 1)
                    return ApiResponse<bool>.ErrorResult(
                        "Cannot remove the last admin from the conversation", 
                        400);
            }

            // 6. Remove participant
            conversation.RemoveParticipant(request.UserIdToRemove);

            // 7. Save conversation
            await _conversationRepository.UpdateAsync(conversation, cancellationToken);

            return ApiResponse<bool>.SuccessResult(true);
        }
        catch (Exception ex)
        {
            return ApiResponse<bool>.ErrorResult(
                $"Failed to remove participant: {ex.Message}", 
                500);
        }
    }
}
