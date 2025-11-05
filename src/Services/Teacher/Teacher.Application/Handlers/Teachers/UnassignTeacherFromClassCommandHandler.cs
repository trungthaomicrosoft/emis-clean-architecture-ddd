using EMIS.BuildingBlocks.ApiResponse;
using EMIS.SharedKernel;
using MediatR;
using Teacher.Application.Commands;
using Teacher.Domain.Repositories;

namespace Teacher.Application.Handlers.Teachers;

public class UnassignTeacherFromClassCommandHandler 
    : IRequestHandler<UnassignTeacherFromClassCommand, ApiResponse<bool>>
{
    private readonly ITeacherRepository _teacherRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UnassignTeacherFromClassCommandHandler(
        ITeacherRepository teacherRepository,
        IUnitOfWork unitOfWork)
    {
        _teacherRepository = teacherRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<bool>> Handle(
        UnassignTeacherFromClassCommand request,
        CancellationToken cancellationToken)
    {
        // Get teacher with assignments
        var teacher = await _teacherRepository.GetByIdWithAssignmentsAsync(
            request.TeacherId,
            cancellationToken);

        if (teacher == null)
        {
            return ApiResponse<bool>.ErrorResult(
                $"Teacher with id {request.TeacherId} not found",
                404);
        }

        try
        {
            // Unassign from class
            teacher.UnassignFromClass(request.ClassId, request.EndDate);

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
