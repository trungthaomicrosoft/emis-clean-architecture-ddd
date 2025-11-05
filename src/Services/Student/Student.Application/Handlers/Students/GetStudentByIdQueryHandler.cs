using AutoMapper;
using EMIS.BuildingBlocks.ApiResponse;
using EMIS.BuildingBlocks.Exceptions;
using MediatR;
using Student.Application.DTOs;
using Student.Application.Queries.Students;
using Student.Domain.Repositories;

namespace Student.Application.Handlers.Students;

/// <summary>
/// Handler for GetStudentByIdQuery
/// </summary>
public class GetStudentByIdQueryHandler : IRequestHandler<GetStudentByIdQuery, ApiResponse<StudentDetailDto>>
{
    private readonly IStudentRepository _studentRepository;
    private readonly IMapper _mapper;

    public GetStudentByIdQueryHandler(IStudentRepository studentRepository, IMapper mapper)
    {
        _studentRepository = studentRepository;
        _mapper = mapper;
    }

    public async Task<ApiResponse<StudentDetailDto>> Handle(GetStudentByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var student = await _studentRepository.GetByIdWithParentsAsync(request.Id, cancellationToken);
            if (student == null)
            {
                throw new NotFoundException($"Không tìm thấy học sinh với ID {request.Id}");
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
            return ApiResponse<StudentDetailDto>.ErrorResult($"Lỗi khi lấy thông tin học sinh: {ex.Message}");
        }
    }
}
