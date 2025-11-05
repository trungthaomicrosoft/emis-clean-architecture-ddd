using EMIS.BuildingBlocks.ApiResponse;
using EMIS.BuildingBlocks.Exceptions;
using MediatR;
using Student.Application.Commands.Students;
using Student.Domain.Repositories;

namespace Student.Application.Handlers.Students;

/// <summary>
/// Handler for DeleteStudentCommand
/// </summary>
public class DeleteStudentCommandHandler : IRequestHandler<DeleteStudentCommand, ApiResponse<bool>>
{
    private readonly IStudentRepository _studentRepository;

    public DeleteStudentCommandHandler(IStudentRepository studentRepository)
    {
        _studentRepository = studentRepository;
    }

    public async Task<ApiResponse<bool>> Handle(DeleteStudentCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var student = await _studentRepository.GetByIdAsync(request.Id, cancellationToken);
            if (student == null)
            {
                throw new NotFoundException($"Không tìm thấy học sinh với ID {request.Id}");
            }

            await _studentRepository.DeleteAsync(student, cancellationToken);
            await _studentRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

            return ApiResponse<bool>.SuccessResult(true);
        }
        catch (NotFoundException ex)
        {
            return ApiResponse<bool>.ErrorResult(ex.Message);
        }
        catch (Exception ex)
        {
            return ApiResponse<bool>.ErrorResult($"Lỗi khi xóa học sinh: {ex.Message}");
        }
    }
}
