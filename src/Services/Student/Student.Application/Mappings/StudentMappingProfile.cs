using AutoMapper;
using Student.Application.DTOs;
using Student.Domain.Entities;
using Student.Domain.Enums;

namespace Student.Application.Mappings;

/// <summary>
/// AutoMapper profile for Student service
/// </summary>
public class StudentMappingProfile : Profile
{
    public StudentMappingProfile()
    {
        // Student mappings
        CreateMap<Domain.Aggregates.Student, StudentDto>()
            .ForMember(d => d.StudentCode, opt => opt.MapFrom(s => s.StudentCode.Value))
            .ForMember(d => d.GenderName, opt => opt.MapFrom(s => s.Gender.ToString()))
            .ForMember(d => d.Age, opt => opt.MapFrom(s => CalculateAge(s.DateOfBirth)))
            .ForMember(d => d.StatusName, opt => opt.MapFrom(s => s.Status.ToString()))
            .ForMember(d => d.ClassName, opt => opt.MapFrom(s => s.Class != null ? s.Class.ClassName : null));

        CreateMap<Domain.Aggregates.Student, StudentDetailDto>()
            .IncludeBase<Domain.Aggregates.Student, StudentDto>()
            .ForMember(d => d.Parents, opt => opt.MapFrom(s => s.Parents));

        // Parent mappings
        CreateMap<Parent, ParentDto>()
            .ForMember(d => d.GenderName, opt => opt.MapFrom(s => s.Gender.ToString()))
            .ForMember(d => d.RelationName, opt => opt.MapFrom(s => s.Relation.ToString()))
            .ForMember(d => d.PhoneNumber, opt => opt.MapFrom(s => s.ContactInfo.PhoneNumber))
            .ForMember(d => d.Email, opt => opt.MapFrom(s => s.ContactInfo.Email));

        // Class mappings
        CreateMap<Class, ClassDto>()
            .ForMember(d => d.StudentCount, opt => opt.MapFrom(s => s.Students.Count));

        // Address mappings
        CreateMap<Domain.ValueObjects.Address, AddressDto>()
            .ForMember(d => d.FullAddress, opt => opt.MapFrom(s => s.GetFullAddress()));
    }

    private static int CalculateAge(DateTime dateOfBirth)
    {
        var age = DateTime.UtcNow.Year - dateOfBirth.Year;
        if (dateOfBirth > DateTime.UtcNow.AddYears(-age)) age--;
        return age;
    }
}
