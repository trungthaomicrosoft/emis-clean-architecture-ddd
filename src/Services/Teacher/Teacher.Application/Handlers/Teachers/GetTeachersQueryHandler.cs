using AutoMapper;
using EMIS.BuildingBlocks.ApiResponse;
using EMIS.BuildingBlocks.Pagination;
using MediatR;
using Teacher.Application.DTOs;
using Teacher.Application.Queries;
using Teacher.Domain.Enums;
using Teacher.Domain.Repositories;

namespace Teacher.Application.Handlers.Teachers;

public class GetTeachersQueryHandler 
    : IRequestHandler<GetTeachersQuery, ApiResponse<PagedResult<TeacherDto>>>
{
    private readonly ITeacherRepository _teacherRepository;
    private readonly IMapper _mapper;

    public GetTeachersQueryHandler(ITeacherRepository teacherRepository, IMapper mapper)
    {
        _teacherRepository = teacherRepository;
        _mapper = mapper;
    }

    public async Task<ApiResponse<PagedResult<TeacherDto>>> Handle(
        GetTeachersQuery request,
        CancellationToken cancellationToken)
    {
        // Convert status
        TeacherStatus? status = request.Status.HasValue 
            ? (TeacherStatus)request.Status.Value 
            : null;

        // Get paged data from repository
        var (items, totalCount) = await _teacherRepository.GetPagedAsync(
            request.PageNumber,
            request.PageSize,
            request.SearchTerm,
            status,
            cancellationToken);

        // Map to DTOs
        var dtos = _mapper.Map<List<TeacherDto>>(items);

        // Create paged result
        var pagedResult = new PagedResult<TeacherDto>(
            dtos,
            totalCount,
            request.PageNumber,
            request.PageSize);

        return ApiResponse<PagedResult<TeacherDto>>.SuccessResult(pagedResult);
    }
}
