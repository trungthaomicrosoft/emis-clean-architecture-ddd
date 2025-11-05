using AutoMapper;
using EMIS.BuildingBlocks.ApiResponse;
using MediatR;
using Teacher.Application.DTOs;
using Teacher.Application.Queries;
using Teacher.Domain.Repositories;

namespace Teacher.Application.Handlers.Teachers;

public class GetTeacherByIdQueryHandler 
    : IRequestHandler<GetTeacherByIdQuery, ApiResponse<TeacherDetailDto>>
{
    private readonly ITeacherRepository _teacherRepository;
    private readonly IMapper _mapper;

    public GetTeacherByIdQueryHandler(ITeacherRepository teacherRepository, IMapper mapper)
    {
        _teacherRepository = teacherRepository;
        _mapper = mapper;
    }

    public async Task<ApiResponse<TeacherDetailDto>> Handle(
        GetTeacherByIdQuery request,
        CancellationToken cancellationToken)
    {
        // Get teacher with assignments
        var teacher = await _teacherRepository.GetByIdWithAssignmentsAsync(
            request.TeacherId,
            cancellationToken);

        if (teacher == null)
        {
            return ApiResponse<TeacherDetailDto>.ErrorResult(
                $"Teacher with id {request.TeacherId} not found",
                404);
        }

        // Map to DTO
        var dto = _mapper.Map<TeacherDetailDto>(teacher);

        return ApiResponse<TeacherDetailDto>.SuccessResult(dto);
    }
}
