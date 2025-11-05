using AutoMapper;
using EMIS.BuildingBlocks.ApiResponse;
using EMIS.SharedKernel;
using Identity.Application.Commands;
using Identity.Application.DTOs;
using Identity.Application.Services;
using Identity.Domain.Enums;
using Identity.Domain.Repositories;
using MediatR;

namespace Identity.Application.Handlers.Auth;

public class LoginCommandHandler : IRequestHandler<LoginCommand, ApiResponse<AuthResponseDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtService _jwtService;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;

    public LoginCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtService jwtService,
        IMapper mapper,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtService = jwtService;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<AuthResponseDto>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Get user by phone number
            var user = await _userRepository.GetByPhoneNumberAsync(request.PhoneNumber, cancellationToken);
            if (user == null)
                return ApiResponse<AuthResponseDto>.ErrorResult("INVALID_CREDENTIALS", "Invalid phone number or password");

            // Check if password is set
            if (user.Status == UserStatus.PendingActivation || string.IsNullOrEmpty(user.PasswordHash))
                return ApiResponse<AuthResponseDto>.ErrorResult("PASSWORD_NOT_SET", "Please set your password first");

            // Check user status
            if (user.Status != UserStatus.Active)
                return ApiResponse<AuthResponseDto>.ErrorResult("ACCOUNT_DISABLED", $"Account is {user.Status}");

            // Verify password
            if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
                return ApiResponse<AuthResponseDto>.ErrorResult("INVALID_CREDENTIALS", "Invalid phone number or password");

            // Generate tokens
            var accessToken = _jwtService.GenerateAccessToken(user);
            var refreshToken = _jwtService.GenerateRefreshToken();

            // Save refresh token
            user.GenerateRefreshToken(refreshToken, expiryDays: 7);
            user.RecordLogin();

            _userRepository.Update(user);
            await _unitOfWork.SaveEntitiesAsync(cancellationToken);

            // Map response
            var userDto = _mapper.Map<UserDto>(user);
            var response = new AuthResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddHours(1), // Access token expiry
                User = userDto
            };

            return ApiResponse<AuthResponseDto>.SuccessResult(response);
        }
        catch (Exception ex)
        {
            return ApiResponse<AuthResponseDto>.ErrorResult("LOGIN_FAILED", ex.Message);
        }
    }
}
