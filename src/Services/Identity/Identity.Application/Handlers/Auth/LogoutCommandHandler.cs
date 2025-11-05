using EMIS.BuildingBlocks.ApiResponse;
using EMIS.SharedKernel;
using Identity.Application.Commands;
using Identity.Domain.Repositories;
using MediatR;

namespace Identity.Application.Handlers.Auth;

public class LogoutCommandHandler : IRequestHandler<LogoutCommand, ApiResponse<bool>>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public LogoutCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<bool>> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Get user with refresh tokens
            var user = await _userRepository.GetByIdWithRefreshTokensAsync(request.UserId, cancellationToken);
            if (user == null)
                return ApiResponse<bool>.ErrorResult("USER_NOT_FOUND", "User not found");

            // Revoke all refresh tokens
            user.RevokeAllRefreshTokens();

            _userRepository.Update(user);
            await _unitOfWork.SaveEntitiesAsync(cancellationToken);

            return ApiResponse<bool>.SuccessResult(true);
        }
        catch (Exception ex)
        {
            return ApiResponse<bool>.ErrorResult("LOGOUT_FAILED", ex.Message);
        }
    }
}
