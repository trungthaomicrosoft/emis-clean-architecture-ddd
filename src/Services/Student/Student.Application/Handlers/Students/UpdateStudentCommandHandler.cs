using AutoMapper;
using EMIS.BuildingBlocks.ApiResponse;
using EMIS.BuildingBlocks.Exceptions;
using MediatR;
using Student.Application.Commands.Students;
using Student.Application.DTOs;
using Student.Domain.Enums;
using Student.Domain.Repositories;
using Student.Domain.ValueObjects;

namespace Student.Application.Handlers.Students;

/// <summary>
/// Handler for UpdateStudentCommand
/// </summary>
public class UpdateStudentCommandHandler : IRequestHandler<UpdateStudentCommand, ApiResponse<StudentDetailDto>>
{
    private readonly IStudentRepository _studentRepository;
    private readonly IClassRepository _classRepository;
    private readonly IMapper _mapper;

    public UpdateStudentCommandHandler(
        IStudentRepository studentRepository,
        IClassRepository classRepository,
        IMapper mapper)
    {
        _studentRepository = studentRepository;
        _classRepository = classRepository;
        _mapper = mapper;
    }

    public async Task<ApiResponse<StudentDetailDto>> Handle(UpdateStudentCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Get existing student
            var student = await _studentRepository.GetByIdWithParentsAsync(request.Id, cancellationToken);
            if (student == null)
            {
                throw new NotFoundException($"Không tìm thấy học sinh với ID {request.Id}");
            }

            // Validate class if changed
            if (request.ClassId.HasValue && request.ClassId != student.ClassId)
            {
                var classEntity = await _classRepository.GetByIdAsync(request.ClassId.Value, cancellationToken);
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

                student.AssignToClass(classEntity.Id, classEntity);
            }
            else if (!request.ClassId.HasValue && student.ClassId.HasValue)
            {
                student.RemoveFromClass();
            }

            // Update student info
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
            student.UpdateHealthInfo(request.HealthNotes, request.Allergies);

            // Save changes
            await _studentRepository.UpdateAsync(student, cancellationToken);
            await _studentRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

            // Get updated student
            var updatedStudent = await _studentRepository.GetByIdWithParentsAsync(student.Id, cancellationToken);
            var result = _mapper.Map<StudentDetailDto>(updatedStudent);

            return ApiResponse<StudentDetailDto>.SuccessResult(result);
        }
        catch (Exception ex) when (ex is BusinessRuleValidationException || ex is NotFoundException)
        {
            return ApiResponse<StudentDetailDto>.ErrorResult(ex.Message);
        }
        catch (Exception ex)
        {
            return ApiResponse<StudentDetailDto>.ErrorResult($"Lỗi khi cập nhật học sinh: {ex.Message}");
        }
    }
}
