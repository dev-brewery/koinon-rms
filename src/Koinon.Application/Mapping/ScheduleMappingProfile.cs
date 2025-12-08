using AutoMapper;
using Koinon.Application.DTOs;
using Koinon.Application.DTOs.Requests;
using Koinon.Domain.Entities;

namespace Koinon.Application.Mapping;

/// <summary>
/// AutoMapper profile for Schedule-related entity mappings.
/// </summary>
public class ScheduleMappingProfile : Profile
{
    public ScheduleMappingProfile()
    {
        // Entity to DTO mappings
        CreateMap<Schedule, ScheduleSummaryDto>()
            .ForMember(d => d.IdKey, o => o.MapFrom(s => s.IdKey));

        // Request to Entity mappings
        CreateMap<CreateScheduleRequest, Schedule>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.Guid, o => o.Ignore())
            .ForMember(d => d.CreatedDateTime, o => o.Ignore())
            .ForMember(d => d.ModifiedDateTime, o => o.Ignore())
            .ForMember(d => d.CategoryId, o => o.Ignore())
            .ForMember(d => d.Groups, o => o.Ignore());
    }
}
