using AutoMapper;
using EMIS.BuildingBlocks.ApiResponse;
using EMIS.SharedKernel;
using Identity.Application.Commands;
using Identity.Domain.Repositories;
using Identity.Domain.ValueObjects;
using MediatR;

namespace Identity.Application.Handlers.Auth;

public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, ApiResponse<Guid>>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateUserCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<Guid>> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Check if phone number already exists
            var phoneExists = await _userRepository.PhoneNumberExistsAsync(request.PhoneNumber, cancellationToken);
            if (phoneExists)
                return ApiResponse<Guid>.ErrorResult("PHONE_EXISTS", "Phone number already registered");

            // Create phone number value object
            var phoneNumber = PhoneNumber.Create(request.PhoneNumber);

            // TODO: Get TenantId from context
            var tenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");

            // Create user (PendingActivation status - no password yet)
            var user = new Domain.Aggregates.User(
                tenantId,
                phoneNumber,
                request.FullName,
                request.Role,
                request.EntityId,
                request.Email);

            // Add to repository
            await _userRepository.AddAsync(user, cancellationToken);
            await _unitOfWork.SaveEntitiesAsync(cancellationToken);

            return ApiResponse<Guid>.SuccessResult(user.Id);
        }
        catch (ArgumentException ex)
        {
            return ApiResponse<Guid>.ErrorResult("VALIDATION_ERROR", ex.Message);
        }
        catch (Exception ex)
        {
            return ApiResponse<Guid>.ErrorResult("CREATE_USER_FAILED", ex.Message);
        }
    }
}
