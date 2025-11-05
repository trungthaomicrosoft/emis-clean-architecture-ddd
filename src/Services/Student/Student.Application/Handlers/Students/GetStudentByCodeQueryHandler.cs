using AutoMapper;
using EMIS.BuildingBlocks.ApiResponse;
using EMIS.BuildingBlocks.Exceptions;
using MediatR;
using Student.Application.DTOs;
using Student.Application.Queries.Students;
using Student.Domain.Repositories;
using Student.Domain.ValueObjects;

namespace Student.Application.Handlers.Students;

/// <summary>
/// Handler for GetStudentByCodeQuery
/// </summary>
public class GetStudentByCodeQueryHandler : IRequestHandler<GetStudentByCodeQuery, ApiResponse<StudentDetailDto>>
{
    private readonly IStudentRepository _studentRepository;
    private readonly IMapper _mapper;

    public GetStudentByCodeQueryHandler(IStudentRepository studentRepository, IMapper mapper)
    {
        _studentRepository = studentRepository;
        _mapper = mapper;
    }

    public async Task<ApiResponse<StudentDetailDto>> Handle(GetStudentByCodeQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var studentCode = StudentCode.Create(request.StudentCode);
            var student = await _studentRepository.GetByCodeAsync(studentCode, cancellationToken);
            
            if (student == null)
            {
                throw new NotFoundException($"Không tìm thấy học sinh với mã {request.StudentCode}");
            }

            var result = _mapper.Map<StudentDetailDto>(student);
            return ApiResponse<StudentDetailDto>.SuccessResult(result);
        }
        catch (NotFoundException ex)
        {
            return ApiResponse<StudentDetailDto>.ErrorResult(ex.Message);
        }
        catch (Exception ex)
        {
            return ApiResponse<StudentDetailDto>.ErrorResult($"Lỗi khi tìm học sinh: {ex.Message}");
        }
    }
}
