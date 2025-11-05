using AutoMapper;
using EMIS.BuildingBlocks.ApiResponse;
using EMIS.BuildingBlocks.Pagination;
using MediatR;
using Student.Application.DTOs;
using Student.Application.Queries.Students;
using Student.Domain.Repositories;

namespace Student.Application.Handlers.Students;

/// <summary>
/// Handler for GetStudentsQuery with pagination
/// Uses repository method to encapsulate query logic (DDD compliant)
/// </summary>
public class GetStudentsQueryHandler : IRequestHandler<GetStudentsQuery, ApiResponse<PagedResult<StudentDto>>>
{
    private readonly IStudentRepository _studentRepository;
    private readonly IMapper _mapper;

    public GetStudentsQueryHandler(IStudentRepository studentRepository, IMapper mapper)
    {
        _studentRepository = studentRepository;
        _mapper = mapper;
    }

    public async Task<ApiResponse<PagedResult<StudentDto>>> Handle(GetStudentsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // Use repository method with encapsulated query logic
            var (items, totalCount) = await _studentRepository.GetPagedAsync(
                request.PageNumber,
                request.PageSize,
                request.SearchTerm,
                request.Status.HasValue ? (Domain.Enums.StudentStatus)request.Status.Value : null,
                request.ClassId,
                cancellationToken);

            // Map to DTOs
            var dtos = _mapper.Map<List<StudentDto>>(items);

            var pagedResult = new PagedResult<StudentDto>(
                dtos,
                totalCount,
                request.PageNumber,
                request.PageSize
            );

            return ApiResponse<PagedResult<StudentDto>>.SuccessResult(pagedResult);
        }
        catch (Exception ex)
        {
            return ApiResponse<PagedResult<StudentDto>>.ErrorResult($"Lỗi khi lấy danh sách học sinh: {ex.Message}");
        }
    }
}
