using EMIS.BuildingBlocks.ApiResponse;
using EMIS.BuildingBlocks.Exceptions;
using MediatR;
using Student.Application.Commands.Students;
using Student.Domain.Enums;
using Student.Domain.Repositories;

namespace Student.Application.Handlers.Students;

/// <summary>
/// Handler for ChangeStudentStatusCommand
/// </summary>
public class ChangeStudentStatusCommandHandler : IRequestHandler<ChangeStudentStatusCommand, ApiResponse<bool>>
{
    private readonly IStudentRepository _studentRepository;

    public ChangeStudentStatusCommandHandler(IStudentRepository studentRepository)
    {
        _studentRepository = studentRepository;
    }

    public async Task<ApiResponse<bool>> Handle(ChangeStudentStatusCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var student = await _studentRepository.GetByIdAsync(request.Id, cancellationToken);
            if (student == null)
            {
                throw new NotFoundException($"Không tìm thấy học sinh với ID {request.Id}");
            }

            var newStatus = (StudentStatus)request.Status;
            student.ChangeStatus(newStatus);

            await _studentRepository.UpdateAsync(student, cancellationToken);
            await _studentRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

            return ApiResponse<bool>.SuccessResult(true);
        }
        catch (Exception ex) when (ex is BusinessRuleValidationException || ex is NotFoundException)
        {
            return ApiResponse<bool>.ErrorResult(ex.Message);
        }
        catch (Exception ex)
        {
            return ApiResponse<bool>.ErrorResult($"Lỗi khi thay đổi trạng thái: {ex.Message}");
        }
    }
}
