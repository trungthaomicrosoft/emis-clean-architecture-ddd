using AutoMapper;
using EMIS.BuildingBlocks.ApiResponse;
using EMIS.SharedKernel;
using MediatR;
using Teacher.Application.Commands;
using Teacher.Application.DTOs;
using Teacher.Domain.Enums;
using Teacher.Domain.Repositories;
using Teacher.Domain.ValueObjects;

namespace Teacher.Application.Handlers.Teachers;

public class UpdateTeacherCommandHandler 
    : IRequestHandler<UpdateTeacherCommand, ApiResponse<TeacherDetailDto>>
{
    private readonly ITeacherRepository _teacherRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public UpdateTeacherCommandHandler(
        ITeacherRepository teacherRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _teacherRepository = teacherRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<ApiResponse<TeacherDetailDto>> Handle(
        UpdateTeacherCommand request,
        CancellationToken cancellationToken)
    {
        // Get teacher
        var teacher = await _teacherRepository.GetByIdAsync(request.TeacherId, cancellationToken);
        if (teacher == null)
        {
            return ApiResponse<TeacherDetailDto>.ErrorResult(
                $"Teacher with id {request.TeacherId} not found",
                404);
        }

        // Create address value object
        Address? address = null;
        if (request.Address != null)
        {
            address = Address.Create(
                request.Address.Street,
                request.Address.Ward,
                request.Address.District,
                request.Address.City);
        }

        // Update info
        teacher.UpdateInfo(
            request.FullName,
            (Gender)request.Gender,
            request.DateOfBirth,
            request.Email,
            address);

        // Save changes
        _teacherRepository.Update(teacher);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Map to DTO
        var dto = _mapper.Map<TeacherDetailDto>(teacher);

        return ApiResponse<TeacherDetailDto>.SuccessResult(dto);
    }
}
