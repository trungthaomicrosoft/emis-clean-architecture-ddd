using AutoMapper;
using EMIS.BuildingBlocks.ApiResponse;
using EMIS.SharedKernel;
using Identity.Application.Commands;
using Identity.Application.DTOs;
using Identity.Application.Services;
using Identity.Domain.Repositories;
using MediatR;

namespace Identity.Application.Handlers.Auth;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, ApiResponse<AuthResponseDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtService _jwtService;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;

    public RefreshTokenCommandHandler(
        IUserRepository userRepository,
        IJwtService jwtService,
        IMapper mapper,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _jwtService = jwtService;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<AuthResponseDto>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Get user by refresh token
            var user = await _userRepository.GetByRefreshTokenAsync(request.RefreshToken, cancellationToken);
            if (user == null)
                return ApiResponse<AuthResponseDto>.ErrorResult("INVALID_TOKEN", "Invalid refresh token");

            // Find the refresh token entity
            var refreshToken = user.RefreshTokens.FirstOrDefault(t => t.Token == request.RefreshToken);
            if (refreshToken == null || !refreshToken.IsActive())
                return ApiResponse<AuthResponseDto>.ErrorResult("INVALID_TOKEN", "Refresh token is expired or revoked");

            // Generate new tokens
            var newAccessToken = _jwtService.GenerateAccessToken(user);
            var newRefreshToken = _jwtService.GenerateRefreshToken();

            // Revoke old refresh token and create new one
            refreshToken.Revoke();
            user.GenerateRefreshToken(newRefreshToken, expiryDays: 7);

            _userRepository.Update(user);
            await _unitOfWork.SaveEntitiesAsync(cancellationToken);

            // Map response
            var userDto = _mapper.Map<UserDto>(user);
            var response = new AuthResponseDto
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                ExpiresAt = DateTime.UtcNow.AddHours(1),
                User = userDto
            };

            return ApiResponse<AuthResponseDto>.SuccessResult(response);
        }
        catch (Exception ex)
        {
            return ApiResponse<AuthResponseDto>.ErrorResult("REFRESH_FAILED", ex.Message);
        }
    }
}
