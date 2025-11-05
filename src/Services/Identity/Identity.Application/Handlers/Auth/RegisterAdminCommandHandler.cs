using AutoMapper;
using EMIS.BuildingBlocks.ApiResponse;
using EMIS.SharedKernel;
using Identity.Application.Commands;
using Identity.Application.Services;
using Identity.Domain.Aggregates;
using Identity.Domain.Repositories;
using Identity.Domain.ValueObjects;
using MediatR;

namespace Identity.Application.Handlers.Auth;

public class RegisterAdminCommandHandler : IRequestHandler<RegisterAdminCommand, ApiResponse<Guid>>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;

    public RegisterAdminCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<Guid>> Handle(RegisterAdminCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Check if phone number already exists
            var phoneExists = await _userRepository.PhoneNumberExistsAsync(request.PhoneNumber, cancellationToken);
            if (phoneExists)
                return ApiResponse<Guid>.ErrorResult("PHONE_EXISTS", "Phone number already registered");

            // Create phone number value object
            var phoneNumber = PhoneNumber.Create(request.PhoneNumber);

            // Hash password
            var passwordHash = _passwordHasher.HashPassword(request.Password);

            // TODO: Get TenantId from context (hardcoded for now)
            var tenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");

            // Create School Admin user
            var user = new User(
                tenantId,
                phoneNumber,
                request.FullName,
                passwordHash,
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
            return ApiResponse<Guid>.ErrorResult("REGISTER_FAILED", ex.Message);
        }
    }
}
