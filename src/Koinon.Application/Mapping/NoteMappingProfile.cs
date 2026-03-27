using AutoMapper;
using Koinon.Application.DTOs;
using Koinon.Domain.Data;
using Koinon.Domain.Entities;

namespace Koinon.Application.Mapping;

/// <summary>
/// AutoMapper profile for Note entity mappings.
/// </summary>
public class NoteMappingProfile : Profile
{
    public NoteMappingProfile()
    {
        CreateMap<Note, NoteDto>()
            .ForMember(d => d.IdKey, o => o.MapFrom(s => s.IdKey))
            .ForMember(d => d.NoteTypeValueIdKey, o => o.MapFrom(s => IdKeyHelper.Encode(s.NoteTypeValueId)))
            .ForMember(d => d.NoteTypeName, o => o.MapFrom(s => s.NoteTypeValue != null ? s.NoteTypeValue.Value : string.Empty))
            .ForMember(d => d.AuthorPersonIdKey, o => o.MapFrom(s =>
                s.AuthorPersonAlias != null && s.AuthorPersonAlias.Person != null
                    ? s.AuthorPersonAlias.Person.IdKey
                    : null))
            .ForMember(d => d.AuthorPersonName, o => o.MapFrom(s =>
                s.AuthorPersonAlias != null && s.AuthorPersonAlias.Person != null
                    ? s.AuthorPersonAlias.Person.FullName
                    : null));
    }
}
