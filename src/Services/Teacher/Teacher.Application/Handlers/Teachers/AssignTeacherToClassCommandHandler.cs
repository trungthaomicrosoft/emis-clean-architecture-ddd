using EMIS.BuildingBlocks.ApiResponse;
using EMIS.SharedKernel;
using MediatR;
using Teacher.Application.Commands;
using Teacher.Domain.Enums;
using Teacher.Domain.Repositories;

namespace Teacher.Application.Handlers.Teachers;

public class AssignTeacherToClassCommandHandler 
    : IRequestHandler<AssignTeacherToClassCommand, ApiResponse<bool>>
{
    private readonly ITeacherRepository _teacherRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AssignTeacherToClassCommandHandler(
        ITeacherRepository teacherRepository,
        IUnitOfWork unitOfWork)
    {
        _teacherRepository = teacherRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<bool>> Handle(
        AssignTeacherToClassCommand request,
        CancellationToken cancellationToken)
    {
        // Get teacher
        var teacher = await _teacherRepository.GetByIdWithAssignmentsAsync(
            request.TeacherId,
            cancellationToken);

        if (teacher == null)
        {
            return ApiResponse<bool>.ErrorResult(
                $"Teacher with id {request.TeacherId} not found",
                404);
        }

        // Check if assigning as Primary teacher
        if (request.Role == (int)ClassAssignmentRole.Primary)
        {
            // Check if class already has a primary teacher
            var existingPrimaryTeacher = await _teacherRepository.GetPrimaryTeacherByClassAsync(
                request.ClassId,
                cancellationToken);

            if (existingPrimaryTeacher != null && existingPrimaryTeacher.Id != request.TeacherId)
            {
                return ApiResponse<bool>.ErrorResult(
                    $"Class {request.ClassName} already has a primary teacher: {existingPrimaryTeacher.FullName}",
                    400);
            }
        }

        try
        {
            // Assign to class
            teacher.AssignToClass(
                request.ClassId,
                request.ClassName,
                (ClassAssignmentRole)request.Role,
                request.StartDate);

            // Save changes
            _teacherRepository.Update(teacher);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return ApiResponse<bool>.SuccessResult(true);
        }
        catch (Exception ex)
        {
            return ApiResponse<bool>.ErrorResult(ex.Message, 400);
        }
    }
}
