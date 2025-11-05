using AutoMapper;
using EMIS.BuildingBlocks.ApiResponse;
using EMIS.BuildingBlocks.Exceptions;
using EMIS.EventBus;
using EMIS.EventBus.IntegrationEvents;
using EMIS.SharedKernel;
using MediatR;
using Microsoft.Extensions.Logging;
using Teacher.Application.Commands;
using Teacher.Application.DTOs;
using Teacher.Domain.Enums;
using Teacher.Domain.Repositories;
using Teacher.Domain.ValueObjects;

namespace Teacher.Application.Handlers.Teachers;

public class CreateTeacherCommandHandler 
    : IRequestHandler<CreateTeacherCommand, ApiResponse<TeacherDetailDto>>
{
    private readonly ITeacherRepository _teacherRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IKafkaEventBus _eventBus;
    private readonly ILogger<CreateTeacherCommandHandler> _logger;

    public CreateTeacherCommandHandler(
        ITeacherRepository teacherRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IKafkaEventBus eventBus,
        ILogger<CreateTeacherCommandHandler> logger)
    {
        _teacherRepository = teacherRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task<ApiResponse<TeacherDetailDto>> Handle(
        CreateTeacherCommand request,
        CancellationToken cancellationToken)
    {
        // Check phone number uniqueness
        if (await _teacherRepository.ExistsPhoneNumberAsync(request.PhoneNumber, null, cancellationToken))
        {
            return ApiResponse<TeacherDetailDto>.ErrorResult(
                $"Phone number {request.PhoneNumber} already exists",
                400);
        }

        // TODO: Get current tenant from ITenantContext
        var tenantId = Guid.Parse("00000000-0000-0000-0000-000000000001"); // Placeholder

        // Create address value object
        Address? address = null;
        if (request.Address != null)
        {
            address = Address.Create(
                request.Address.Street,
                request.Address.Ward,
                request.Address.District,
                request.Address.City);
        }

        // Create teacher aggregate
        var teacher = new Domain.Aggregates.Teacher(
            tenantId,
            request.UserId,
            request.FullName,
            (Gender)request.Gender,
            request.PhoneNumber,
            request.HireDate);

        // Update additional info
        if (request.DateOfBirth.HasValue || !string.IsNullOrWhiteSpace(request.Email) || address != null)
        {
            teacher.UpdateInfo(
                request.FullName,
                (Gender)request.Gender,
                request.DateOfBirth,
                request.Email,
                address);
        }

        // Save to database
        await _teacherRepository.AddAsync(teacher, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Publish integration event for Identity Service
        try
        {
            var integrationEvent = new TeacherCreatedIntegrationEvent(
                teacher.Id,
                teacher.TenantId,
                teacher.PhoneNumber,
                teacher.FullName,
                teacher.Email);

            await _eventBus.PublishAsync(integrationEvent, cancellationToken);
            
            _logger.LogInformation(
                "Published TeacherCreatedIntegrationEvent for Teacher {TeacherId}", 
                teacher.Id);
        }
        catch (Exception ex)
        {
            // Log error but don't fail the operation
            // The event can be republished via retry mechanism or manual intervention
            _logger.LogError(ex, 
                "Failed to publish TeacherCreatedIntegrationEvent for Teacher {TeacherId}. Event will need to be republished.", 
                teacher.Id);
        }

        // Map to DTO
        var dto = _mapper.Map<TeacherDetailDto>(teacher);

        return ApiResponse<TeacherDetailDto>.SuccessResult(dto);
    }
}
