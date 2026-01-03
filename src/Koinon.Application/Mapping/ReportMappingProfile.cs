using AutoMapper;
using Koinon.Application.DTOs.Reports;
using Koinon.Domain.Data;
using Koinon.Domain.Entities;

namespace Koinon.Application.Mapping;

/// <summary>
/// AutoMapper profile for Report-related entity mappings.
/// </summary>
public class ReportMappingProfile : Profile
{
    public ReportMappingProfile()
    {
        // ReportDefinition mappings
        CreateMap<ReportDefinition, ReportDefinitionDto>()
            .ForMember(d => d.IdKey, o => o.MapFrom(s => s.IdKey));

        CreateMap<CreateReportDefinitionRequest, ReportDefinition>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.Guid, o => o.Ignore())
            .ForMember(d => d.IdKey, o => o.Ignore())
            .ForMember(d => d.CreatedDateTime, o => o.Ignore())
            .ForMember(d => d.ModifiedDateTime, o => o.Ignore())
            .ForMember(d => d.IsActive, o => o.MapFrom(_ => true))
            .ForMember(d => d.IsSystem, o => o.MapFrom(_ => false))
            .ForMember(d => d.ParameterSchema, o => o.MapFrom(s => s.ParameterSchema ?? "{}"))
            .ForMember(d => d.DefaultParameters, o => o.MapFrom(s => s.DefaultParameters));

        // ReportRun mappings
        CreateMap<ReportRun, ReportRunDto>()
            .ForMember(d => d.IdKey, o => o.MapFrom(s => s.IdKey))
            .ForMember(d => d.ReportDefinitionIdKey, o => o.MapFrom(s => IdKeyHelper.Encode(s.ReportDefinitionId)))
            .ForMember(d => d.ReportName, o => o.MapFrom(s => s.ReportDefinition.Name))
            .ForMember(d => d.OutputFileIdKey, o => o.MapFrom(s => s.OutputFileId.HasValue ? IdKeyHelper.Encode(s.OutputFileId.Value) : null))
            .ForMember(d => d.RequestedByName, o => o.MapFrom(s => s.RequestedByPersonAlias != null && s.RequestedByPersonAlias.Person != null ? s.RequestedByPersonAlias.Person.FullName : null));

        // ReportSchedule mappings
        CreateMap<ReportSchedule, ReportScheduleDto>()
            .ForMember(d => d.IdKey, o => o.MapFrom(s => s.IdKey))
            .ForMember(d => d.ReportDefinitionIdKey, o => o.MapFrom(s => IdKeyHelper.Encode(s.ReportDefinitionId)))
            .ForMember(d => d.ReportName, o => o.MapFrom(s => s.ReportDefinition.Name));

        CreateMap<CreateReportScheduleRequest, ReportSchedule>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.Guid, o => o.Ignore())
            .ForMember(d => d.IdKey, o => o.Ignore())
            .ForMember(d => d.CreatedDateTime, o => o.Ignore())
            .ForMember(d => d.ModifiedDateTime, o => o.Ignore())
            .ForMember(d => d.ReportDefinitionId, o => o.Ignore()) // Set manually
            .ForMember(d => d.IsActive, o => o.MapFrom(_ => true))
            .ForMember(d => d.LastRunAt, o => o.Ignore())
            .ForMember(d => d.NextRunAt, o => o.Ignore())
            .ForMember(d => d.ReportDefinition, o => o.Ignore())
            .ForMember(d => d.Parameters, o => o.MapFrom(s => s.Parameters ?? "{}"))
            .ForMember(d => d.RecipientPersonAliasIds, o => o.MapFrom(s => s.RecipientPersonAliasIds ?? "[]"));
    }
}
