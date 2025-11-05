using AutoMapper;
using EMIS.BuildingBlocks.ApiResponse;
using EMIS.BuildingBlocks.Pagination;
using MediatR;
using Student.Application.DTOs;
using Student.Application.Queries.Students;
using Student.Domain.Repositories;

namespace Student.Application.Handlers.Students;

/// <summary>
/// Handler xử lý query lấy danh sách học sinh đã có phụ huynh
/// Uses repository method to encapsulate query logic (DDD compliant)
/// </summary>
public class GetStudentsWithParentsQueryHandler 
    : IRequestHandler<GetStudentsWithParentsQuery, ApiResponse<PagedResult<StudentDto>>>
{
    private readonly IStudentRepository _studentRepository;
    private readonly IMapper _mapper;

    public GetStudentsWithParentsQueryHandler(
        IStudentRepository studentRepository,
        IMapper mapper)
    {
        _studentRepository = studentRepository;
        _mapper = mapper;
    }

    public async Task<ApiResponse<PagedResult<StudentDto>>> Handle(
        GetStudentsWithParentsQuery request, 
        CancellationToken cancellationToken)
    {
        try
        {
            // Use repository method with encapsulated query logic
            var (items, totalCount) = await _studentRepository.GetStudentsWithParentsPagedAsync(
                request.PageNumber,
                request.PageSize,
                request.MinParentCount,
                request.SearchTerm,
                request.Status,
                request.ClassId,
                cancellationToken);

            // Map sang DTO
            var studentDtos = _mapper.Map<List<StudentDto>>(items);

            // Tạo PagedResult
            var pagedResult = new PagedResult<StudentDto>(
                studentDtos,
                totalCount,
                request.PageNumber,
                request.PageSize);

            return ApiResponse<PagedResult<StudentDto>>.SuccessResult(
                pagedResult, 
                $"Tìm thấy {totalCount} học sinh đã có phụ huynh");
        }
        catch (Exception)
        {
            return ApiResponse<PagedResult<StudentDto>>.ErrorResult(
                "Có lỗi xảy ra khi lấy danh sách học sinh có phụ huynh");
        }
    }
}
