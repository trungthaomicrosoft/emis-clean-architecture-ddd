using AutoMapper;
using Chat.Application.DTOs;
using Chat.Domain.Aggregates;
using Chat.Domain.Entities;
using Chat.Domain.ValueObjects;

namespace Chat.Application.Mappings;

/// <summary>
/// AutoMapper profile for Chat domain models to DTOs
/// </summary>
public class ChatMappingProfile : Profile
{
    public ChatMappingProfile()
    {
        // Conversation mappings
        CreateMap<Conversation, ConversationDto>()
            .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type.ToString()))
            .ForMember(dest => dest.UnreadCount, opt => opt.Ignore()); // Set by query handler per user

        CreateMap<Conversation, ConversationDetailDto>()
            .IncludeBase<Conversation, ConversationDto>();

        CreateMap<ConversationMetadata, ConversationMetadataDto>();

        CreateMap<Participant, ParticipantDto>()
            .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role.ToString()));

        CreateMap<MessageSummary, MessageSummaryDto>();

        // Message mappings
        CreateMap<Message, MessageDto>()
            .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type.ToString()))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()));

        CreateMap<Attachment, AttachmentDto>();
        CreateMap<ReplyToMessage, ReplyToMessageDto>();
        CreateMap<Mention, MentionDto>();
        CreateMap<Reaction, ReactionDto>();
        CreateMap<ReadReceipt, ReadReceiptDto>();

        // Reverse mappings (DTO to ValueObject) for command handling
        CreateMap<AttachmentDto, Attachment>()
            .ConstructUsing(dto => Attachment.Create(dto.FileName, dto.FileType, dto.FileSize, dto.Url, dto.ThumbnailUrl));

        CreateMap<MentionDto, Mention>()
            .ConstructUsing(dto => Mention.Create(dto.UserId, dto.UserName, dto.StartIndex, dto.Length));
    }
}
