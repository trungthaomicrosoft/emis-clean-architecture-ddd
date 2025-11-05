using AutoMapper;
using EMIS.BuildingBlocks.ApiResponse;
using MediatR;
using Student.Application.DTOs;
using Student.Application.Queries.Students;
using Student.Domain.Repositories;

namespace Student.Application.Handlers.Students;

/// <summary>
/// Handler for GetStudentsByClassQuery
/// </summary>
public class GetStudentsByClassQueryHandler : IRequestHandler<GetStudentsByClassQuery, ApiResponse<List<StudentDto>>>
{
    private readonly IStudentRepository _studentRepository;
    private readonly IMapper _mapper;

    public GetStudentsByClassQueryHandler(IStudentRepository studentRepository, IMapper mapper)
    {
        _studentRepository = studentRepository;
        _mapper = mapper;
    }

    public async Task<ApiResponse<List<StudentDto>>> Handle(GetStudentsByClassQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var students = await _studentRepository.GetByClassIdAsync(request.ClassId, cancellationToken);
            var result = _mapper.Map<List<StudentDto>>(students);
            
            return ApiResponse<List<StudentDto>>.SuccessResult(result);
        }
        catch (Exception ex)
        {
            return ApiResponse<List<StudentDto>>.ErrorResult($"Lỗi khi lấy danh sách học sinh: {ex.Message}");
        }
    }
}
