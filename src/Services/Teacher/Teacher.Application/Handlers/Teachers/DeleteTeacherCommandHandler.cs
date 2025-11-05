using EMIS.BuildingBlocks.ApiResponse;
using EMIS.SharedKernel;
using MediatR;
using Teacher.Application.Commands;
using Teacher.Domain.Repositories;

namespace Teacher.Application.Handlers.Teachers;

public class DeleteTeacherCommandHandler 
    : IRequestHandler<DeleteTeacherCommand, ApiResponse<bool>>
{
    private readonly ITeacherRepository _teacherRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteTeacherCommandHandler(
        ITeacherRepository teacherRepository,
        IUnitOfWork unitOfWork)
    {
        _teacherRepository = teacherRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<bool>> Handle(
        DeleteTeacherCommand request,
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

        // Check if has active assignments
        if (teacher.HasActiveAssignments())
        {
            return ApiResponse<bool>.ErrorResult(
                "Cannot delete teacher with active class assignments. Please unassign from all classes first.",
                400);
        }

        // Delete teacher
        _teacherRepository.Delete(teacher);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<bool>.SuccessResult(true);
    }
}
