using AutoMapper;
using EMIS.BuildingBlocks.ApiResponse;
using Identity.Application.DTOs;
using Identity.Application.Queries;
using Identity.Domain.Repositories;
using MediatR;

namespace Identity.Application.Handlers.Users;

public class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, ApiResponse<UserDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;

    public GetUserByIdQueryHandler(IUserRepository userRepository, IMapper mapper)
    {
        _userRepository = userRepository;
        _mapper = mapper;
    }

    public async Task<ApiResponse<UserDto>> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null)
            return ApiResponse<UserDto>.ErrorResult("USER_NOT_FOUND", "User not found");

        var userDto = _mapper.Map<UserDto>(user);
        return ApiResponse<UserDto>.SuccessResult(userDto);
    }
}
