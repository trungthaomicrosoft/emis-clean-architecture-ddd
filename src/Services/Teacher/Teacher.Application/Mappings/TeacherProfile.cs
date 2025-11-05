using AutoMapper;
using Teacher.Application.DTOs;
using Teacher.Domain.Enums;

namespace Teacher.Application.Mappings;

/// <summary>
/// AutoMapper profile cho Teacher Service
/// </summary>
public class TeacherProfile : Profile
{
    public TeacherProfile()
    {
        // Teacher mappings
        CreateMap<Domain.Aggregates.Teacher, TeacherDto>()
            .ForMember(dest => dest.GenderName, opt => opt.MapFrom(src => src.Gender.ToString()))
            .ForMember(dest => dest.StatusName, opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.Age, opt => opt.MapFrom(src => src.GetAge()));

        CreateMap<Domain.Aggregates.Teacher, TeacherDetailDto>()
            .ForMember(dest => dest.GenderName, opt => opt.MapFrom(src => src.Gender.ToString()))
            .ForMember(dest => dest.StatusName, opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.Age, opt => opt.MapFrom(src => src.GetAge()))
            .ForMember(dest => dest.ActiveAssignmentsCount, opt => opt.MapFrom(src => src.GetActiveAssignments().Count()));

        // ClassAssignment mappings
        CreateMap<Domain.Entities.ClassAssignment, ClassAssignmentDto>()
            .ForMember(dest => dest.RoleName, opt => opt.MapFrom(src => src.Role.ToString()));

        // Address mappings
        CreateMap<Domain.ValueObjects.Address, AddressDto>().ReverseMap();
        CreateMap<AddressDto, Domain.ValueObjects.Address>()
            .ConstructUsing(dto => Domain.ValueObjects.Address.Create(
                dto.Street, dto.Ward, dto.District, dto.City));
    }
}
