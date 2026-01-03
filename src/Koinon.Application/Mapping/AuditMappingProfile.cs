using AutoMapper;
using Koinon.Application.DTOs;
using Koinon.Domain.Data;
using Koinon.Domain.Entities;
using System.Text.Json;

namespace Koinon.Application.Mapping;

/// <summary>
/// AutoMapper profile for AuditLog entity mappings.
/// </summary>
public class AuditMappingProfile : Profile
{
    public AuditMappingProfile()
    {
        CreateMap<AuditLog, AuditLogDto>()
            .ForMember(d => d.IdKey, o => o.MapFrom(s => s.IdKey))
            .ForMember(d => d.PersonIdKey, o => o.MapFrom(s => s.PersonId.HasValue ? IdKeyHelper.Encode(s.PersonId.Value) : null))
            .ForMember(d => d.PersonName, o => o.MapFrom(s => s.Person != null ? s.Person.FullName : null))
            .ForMember(d => d.ChangedProperties, o => o.MapFrom(s => DeserializeChangedProperties(s.ChangedProperties)));
    }

    private static List<string>? DeserializeChangedProperties(string? changedProperties)
    {
        if (string.IsNullOrWhiteSpace(changedProperties))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(changedProperties);
        }
        catch
        {
            return null;
        }
    }
}
