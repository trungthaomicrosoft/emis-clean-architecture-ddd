using AutoMapper;
using Chat.Application.DTOs;
using Chat.Domain.Aggregates;
using Chat.Domain.Enums;
using Chat.Domain.Repositories;
using EMIS.BuildingBlocks.ApiResponse;
using MediatR;

namespace Chat.Application.Commands.Conversations;

/// <summary>
/// Handler for creating announcement channel conversations
/// </summary>
public class CreateAnnouncementChannelCommandHandler 
    : IRequestHandler<CreateAnnouncementChannelCommand, ApiResponse<ConversationDto>>
{
    private readonly IConversationRepository _conversationRepository;
    private readonly IMapper _mapper;

    public CreateAnnouncementChannelCommandHandler(
        IConversationRepository conversationRepository,
        IMapper mapper)
    {
        _conversationRepository = conversationRepository;
        _mapper = mapper;
    }

    public async Task<ApiResponse<ConversationDto>> Handle(
        CreateAnnouncementChannelCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            // 1. Validate participants
            if (!request.AdminIds.Any())
                return ApiResponse<ConversationDto>.ErrorResult(
                    "At least one admin is required", 
                    400);

            // 2. TODO: Fetch user names from UserService
            // For now, using placeholder names
            var admins = request.AdminIds
                .Select((adminId, index) => (adminId, $"Admin{index + 1}"))
                .ToList();
            var parents = request.ParentIds
                .Select((parentId, index) => (parentId, $"Parent{index + 1}"))
                .ToList();

            // 3. Create conversation using factory method
            var conversation = Conversation.CreateAnnouncementChannel(
                request.TenantId,
                request.ChannelName,
                request.CreatedBy,
                "Creator", // TODO: Fetch from UserService
                admins,
                parents);

            // 4. Save conversation
            await _conversationRepository.AddAsync(conversation, cancellationToken);

            // 5. Map to DTO and return
            var conversationDto = _mapper.Map<ConversationDto>(conversation);
            return ApiResponse<ConversationDto>.SuccessResult(conversationDto);
        }
        catch (Exception ex)
        {
            return ApiResponse<ConversationDto>.ErrorResult(
                $"Failed to create announcement channel: {ex.Message}", 
                500);
        }
    }
}
