using AutoMapper;
using Koinon.Application.DTOs.Exports;
using Koinon.Domain.Data;
using Koinon.Domain.Entities;

namespace Koinon.Application.Mapping;

/// <summary>
/// AutoMapper profile for Export-related entity mappings.
/// </summary>
public class ExportMappingProfile : Profile
{
    public ExportMappingProfile()
    {
        // ExportJob mappings
        CreateMap<ExportJob, ExportJobDto>()
            .ForMember(d => d.IdKey, o => o.MapFrom(s => s.IdKey))
            .ForMember(d => d.OutputFileIdKey, o => o.MapFrom(s =>
                s.OutputFileId.HasValue ? IdKeyHelper.Encode(s.OutputFileId.Value) : null))
            .ForMember(d => d.FileName, o => o.MapFrom(s => s.OutputFile != null ? s.OutputFile.FileName : null))
            .ForMember(d => d.RequestedByPersonAliasIdKey, o => o.MapFrom(s =>
                s.RequestedByPersonAliasId.HasValue ? IdKeyHelper.Encode(s.RequestedByPersonAliasId.Value) : null));
    }
}
