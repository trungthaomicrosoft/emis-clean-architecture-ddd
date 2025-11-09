using AutoMapper;
using EMIS.BuildingBlocks.ApiResponse;
using EMIS.BuildingBlocks.Exceptions;
using EMIS.BuildingBlocks.MultiTenant;
using EMIS.EventBus;
using EMIS.EventBus.IntegrationEvents;
using MediatR;
using Microsoft.Extensions.Logging;
using Student.Application.Commands.Students;
using Student.Application.DTOs;
using Student.Domain.Entities;
using Student.Domain.Enums;
using Student.Domain.Repositories;
using Student.Domain.ValueObjects;

namespace Student.Application.Handlers.Students;

/// <summary>
/// Handler for CreateStudentCommand
/// </summary>
public class CreateStudentCommandHandler : IRequestHandler<CreateStudentCommand, ApiResponse<StudentDetailDto>>
{
    private readonly IStudentRepository _studentRepository;
    private readonly IClassRepository _classRepository;
    private readonly ITenantContext _tenantContext;
    private readonly IMapper _mapper;
    private readonly IEventBus _eventBus;
    private readonly ILogger<CreateStudentCommandHandler> _logger;

    public CreateStudentCommandHandler(
        IStudentRepository studentRepository,
        IClassRepository classRepository,
        ITenantContext tenantContext,
        IMapper mapper,
        IEventBus eventBus,
        ILogger<CreateStudentCommandHandler> logger)
    {
        _studentRepository = studentRepository;
        _classRepository = classRepository;
        _tenantContext = tenantContext;
        _mapper = mapper;
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task<ApiResponse<StudentDetailDto>> Handle(CreateStudentCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Generate student code
            var year = DateTime.UtcNow.Year;
            var sequence = await _studentRepository.GetNextSequenceNumberAsync(year, cancellationToken);
            var studentCode = StudentCode.Generate(year, sequence);

            // Check if code already exists (race condition check)
            if (await _studentRepository.IsCodeExistsAsync(studentCode, cancellationToken))
            {
                throw new BusinessRuleValidationException($"Mã học sinh {studentCode} đã tồn tại");
            }

            // Validate class if provided
            Domain.Entities.Class? classEntity = null;
            if (request.ClassId.HasValue)
            {
                classEntity = await _classRepository.GetByIdAsync(request.ClassId.Value, cancellationToken);
                if (classEntity == null)
                {
                    throw new NotFoundException($"Không tìm thấy lớp với ID {request.ClassId.Value}");
                }

                if (!classEntity.IsActive)
                {
                    throw new BusinessRuleValidationException($"Lớp {classEntity.ClassName} đang không hoạt động");
                }

                if (await _classRepository.IsClassFullAsync(classEntity.Id, cancellationToken))
                {
                    throw new BusinessRuleValidationException($"Lớp {classEntity.ClassName} đã đầy");
                }
            }

            // Create student aggregate
            var student = new Domain.Aggregates.Student(
                _tenantContext.TenantId,
                studentCode,
                request.FullName,
                (Gender)request.Gender,
                request.DateOfBirth,
                request.EnrollmentDate
            );

            // Update additional info
            var address = request.Address != null
                ? Address.Create(
                    request.Address.Street,
                    request.Address.Ward,
                    request.Address.District,
                    request.Address.City,
                    request.Address.PostalCode)
                : null;

            student.UpdateInfo(
                request.FullName,
                (Gender)request.Gender,
                request.DateOfBirth,
                request.PlaceOfBirth,
                request.Nationality,
                request.EthnicGroup,
                address
            );

            // Update health info
            if (!string.IsNullOrEmpty(request.HealthNotes) || !string.IsNullOrEmpty(request.Allergies))
            {
                student.UpdateHealthInfo(request.HealthNotes, request.Allergies);
            }

            // Assign to class
            if (classEntity != null)
            {
                student.AssignToClass(classEntity.Id, classEntity);
            }

            // Add parents
            foreach (var parentDto in request.Parents)
            {
                var parentAddress = parentDto.Address != null
                    ? Address.Create(
                        parentDto.Address.Street,
                        parentDto.Address.Ward,
                        parentDto.Address.District,
                        parentDto.Address.City,
                        parentDto.Address.PostalCode)
                    : null;

                var contactInfo = ContactInfo.Create(parentDto.PhoneNumber, parentDto.Email);

                var parent = new Parent(
                    _tenantContext.TenantId,
                    student.Id,
                    parentDto.FullName,
                    (Gender)parentDto.Gender,
                    (RelationType)parentDto.Relation,
                    contactInfo,
                    parentDto.IsPrimaryContact
                );

                parent.UpdateInfo(
                    parentDto.FullName,
                    (Gender)parentDto.Gender,
                    parentDto.DateOfBirth,
                    contactInfo,
                    parentAddress,
                    parentDto.Job,
                    parentDto.Workplace
                );

                student.AddParent(parent);
            }

            // Save to repository
            await _studentRepository.AddAsync(student, cancellationToken);
            await _studentRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

            // Get full student data with parents
            var savedStudent = await _studentRepository.GetByIdWithParentsAsync(student.Id, cancellationToken);
            var result = _mapper.Map<StudentDetailDto>(savedStudent);

            // Publish StudentCreatedIntegrationEvent to Kafka
            try
            {
                var integrationEvent = new StudentCreatedIntegrationEvent(
                    student.Id,
                    _tenantContext.TenantId,
                    classEntity?.Id ?? Guid.Empty,
                    student.FullName,
                    savedStudent?.Parents?.Select(p => new ParentInfo(
                        p.Id,
                        p.FullName,
                        p.Relation.ToString()
                    )).ToList() ?? new List<ParentInfo>(),
                    Guid.Empty // CreatedBy - you might want to add this to your command
                );

                await _eventBus.PublishAsync(integrationEvent, cancellationToken);
                
                _logger.LogInformation(
                    "Published StudentCreatedIntegrationEvent for student {StudentId} ({StudentName})",
                    student.Id, student.FullName);
            }
            catch (Exception ex)
            {
                // Log error but don't fail the request - event publishing is not critical
                _logger.LogError(ex,
                    "Failed to publish StudentCreatedIntegrationEvent for student {StudentId}",
                    student.Id);
            }

            return ApiResponse<StudentDetailDto>.SuccessResult(result);
        }
        catch (Exception ex) when (ex is BusinessRuleValidationException || ex is NotFoundException)
        {
            return ApiResponse<StudentDetailDto>.ErrorResult(ex.Message);
        }
        catch (Exception ex)
        {
            return ApiResponse<StudentDetailDto>.ErrorResult($"Lỗi khi tạo học sinh: {ex.Message}");
        }
    }
}
