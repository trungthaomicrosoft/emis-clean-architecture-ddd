using EMIS.BuildingBlocks.ApiResponse;
using EMIS.SharedKernel;
using Identity.Application.Commands;
using Identity.Application.Services;
using Identity.Domain.Repositories;
using MediatR;

namespace Identity.Application.Handlers.Auth;

public class SetPasswordCommandHandler : IRequestHandler<SetPasswordCommand, ApiResponse<bool>>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;

    public SetPasswordCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<bool>> Handle(SetPasswordCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Get user by phone number
            var user = await _userRepository.GetByPhoneNumberAsync(request.PhoneNumber, cancellationToken);
            if (user == null)
                return ApiResponse<bool>.ErrorResult("USER_NOT_FOUND", "User not found");

            // Hash new password
            var passwordHash = _passwordHasher.HashPassword(request.NewPassword);

            // Set password (domain logic handles status validation)
            user.SetPasswordFirstTime(passwordHash);

            _userRepository.Update(user);
            await _unitOfWork.SaveEntitiesAsync(cancellationToken);

            return ApiResponse<bool>.SuccessResult(true);
        }
        catch (InvalidOperationException ex)
        {
            return ApiResponse<bool>.ErrorResult("OPERATION_INVALID", ex.Message);
        }
        catch (Exception ex)
        {
            return ApiResponse<bool>.ErrorResult("SET_PASSWORD_FAILED", ex.Message);
        }
    }
}
