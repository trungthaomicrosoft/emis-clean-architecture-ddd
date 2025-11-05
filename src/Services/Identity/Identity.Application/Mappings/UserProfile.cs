using AutoMapper;
using Identity.Application.DTOs;
using Identity.Domain.Aggregates;

namespace Identity.Application.Mappings;

public class UserProfile : Profile
{
    public UserProfile()
    {
        CreateMap<User, UserDto>()
            .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.PhoneNumber.Value));
    }
}
