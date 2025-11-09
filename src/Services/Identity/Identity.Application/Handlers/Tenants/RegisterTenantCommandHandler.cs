using EMIS.BuildingBlocks.ApiResponse;
using EMIS.SharedKernel;
using Identity.Application.Commands;
using Identity.Application.DTOs;
using Identity.Application.Services;
using Identity.Domain.Aggregates;
using Identity.Domain.Repositories;
using Identity.Domain.ValueObjects;
using MediatR;

namespace Identity.Application.Handlers.Tenants;

/// <summary>
/// Handler: Đăng ký tenant mới (trường học) + School Admin
/// Transaction: Tạo Tenant và User trong cùng 1 transaction
/// </summary>
public class RegisterTenantCommandHandler : IRequestHandler<RegisterTenantCommand, ApiResponse<TenantRegistrationDto>>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;

    public RegisterTenantCommandHandler(
        ITenantRepository tenantRepository,
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork)
    {
        _tenantRepository = tenantRepository;
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<TenantRegistrationDto>> Handle(
        RegisterTenantCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            // 1. Validate subdomain uniqueness
            var subdomainExists = await _tenantRepository.ExistsSubdomainAsync(
                request.Subdomain.ToLowerInvariant(),
                cancellationToken);

            if (subdomainExists)
            {
                return ApiResponse<TenantRegistrationDto>.ErrorResult(
                    "SUBDOMAIN_EXISTS",
                    $"Subdomain '{request.Subdomain}' is already taken. Please choose another.");
            }

            // 2. Validate admin phone number uniqueness (across ALL tenants)
            // Note: Phone numbers are globally unique in this system
            var phoneExists = await _userRepository.PhoneNumberExistsAsync(
                request.AdminPhoneNumber,
                cancellationToken);

            if (phoneExists)
            {
                return ApiResponse<TenantRegistrationDto>.ErrorResult(
                    "ADMIN_PHONE_EXISTS",
                    $"Phone number '{request.AdminPhoneNumber}' is already registered. Please use another.");
            }

            // 3. Create Tenant aggregate
            var subdomain = Subdomain.Create(request.Subdomain);
            var tenant = new Tenant(
                request.SchoolName,
                subdomain,
                request.ContactEmail,
                request.ContactPhone);

            // Set address if provided
            if (!string.IsNullOrWhiteSpace(request.Address))
            {
                tenant.UpdateContactInfo(address: request.Address);
            }

            // 4. Create School Admin user
            var adminPhoneNumber = PhoneNumber.Create(request.AdminPhoneNumber);
            var passwordHash = _passwordHasher.HashPassword(request.AdminPassword);

            var adminUser = new User(
                tenant.Id, // TenantId is the newly created tenant's ID
                adminPhoneNumber,
                request.AdminFullName,
                passwordHash,
                request.AdminEmail);

            // 5. Publish domain event (for integration events)
            tenant.PublishTenantCreatedEvent(adminUser.Id);

            // 6. Save both entities in one transaction
            await _tenantRepository.AddAsync(tenant, cancellationToken);
            await _userRepository.AddAsync(adminUser, cancellationToken);
            await _unitOfWork.SaveEntitiesAsync(cancellationToken);

            // 7. Build response DTO
            var result = new TenantRegistrationDto
            {
                TenantId = tenant.Id,
                TenantName = tenant.Name,
                Subdomain = tenant.Subdomain.Value,
                AccessUrl = $"https://{tenant.Subdomain.Value}.emis.com", // TODO: Get from config
                
                AdminUserId = adminUser.Id,
                AdminPhoneNumber = adminUser.PhoneNumber.Value,
                AdminFullName = adminUser.FullName,
                
                SubscriptionPlan = tenant.SubscriptionPlan.ToString(),
                SubscriptionExpiresAt = tenant.SubscriptionExpiresAt ?? DateTime.UtcNow.AddDays(30),
                MaxUsers = tenant.MaxUsers,
                
                CreatedAt = tenant.CreatedAt
            };

            return ApiResponse<TenantRegistrationDto>.SuccessResult(
                result,
                "Tenant registered successfully! Your admin account is ready to use.");
        }
        catch (ArgumentException ex)
        {
            // Domain validation errors (from value objects, aggregates)
            return ApiResponse<TenantRegistrationDto>.ErrorResult("VALIDATION_ERROR", ex.Message);
        }
        catch (Exception ex)
        {
            // Unexpected errors
            return ApiResponse<TenantRegistrationDto>.ErrorResult(
                "REGISTRATION_FAILED",
                $"Failed to register tenant: {ex.Message}");
        }
    }
}
