using AutoMapper;
using Koinon.Application.DTOs;
using Koinon.Application.DTOs.Requests;
using Koinon.Domain.Entities;

namespace Koinon.Application.Mapping;

/// <summary>
/// AutoMapper profile for Group-related entity mappings.
/// </summary>
public class GroupMappingProfile : Profile
{
    public GroupMappingProfile()
    {
        // Entity to DTO mappings
        CreateMap<Group, GroupDto>()
            .ForMember(d => d.IdKey, o => o.MapFrom(s => s.IdKey))
            .ForMember(d => d.ParentGroup, o => o.Ignore()) // Will be set separately
            .ForMember(d => d.Members, o => o.Ignore()) // Will be set separately
            .ForMember(d => d.ChildGroups, o => o.Ignore()); // Will be set separately

        CreateMap<GroupMember, GroupMemberDto>()
            .ForMember(d => d.IdKey, o => o.MapFrom(s => s.IdKey))
            .ForMember(d => d.Person, o => o.MapFrom(s => s.Person))
            .ForMember(d => d.Role, o => o.MapFrom(s => s.GroupRole))
            .ForMember(d => d.Status, o => o.MapFrom(s => s.GroupMemberStatus.ToString()));

        CreateMap<GroupType, GroupTypeDto>()
            .ForMember(d => d.IdKey, o => o.MapFrom(s => s.IdKey))
            .ForMember(d => d.Roles, o => o.MapFrom(s => s.Roles));

        CreateMap<GroupTypeRole, GroupTypeRoleDto>()
            .ForMember(d => d.IdKey, o => o.MapFrom(s => s.IdKey));

        CreateMap<Group, GroupSummaryDto>()
            .ForMember(d => d.IdKey, o => o.MapFrom(s => s.IdKey))
            .ForMember(d => d.MemberCount, o => o.Ignore()) // Calculated separately
            .ForMember(d => d.GroupTypeName, o => o.MapFrom(s => s.GroupType != null ? s.GroupType.Name : string.Empty));

        CreateMap<GroupMemberRequest, GroupMemberRequestDto>()
            .ForMember(d => d.IdKey, o => o.MapFrom(s => s.IdKey))
            .ForMember(d => d.Requester, o => o.MapFrom(s => s.Person))
            .ForMember(d => d.Group, o => o.MapFrom(s => s.Group))
            .ForMember(d => d.Status, o => o.MapFrom(s => s.Status.ToString()))
            .ForMember(d => d.ProcessedByPerson, o => o.MapFrom(s => s.ProcessedByPerson));

        // Request to Entity mappings
        CreateMap<CreateGroupRequest, Group>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.Guid, o => o.Ignore())
            .ForMember(d => d.CreatedDateTime, o => o.Ignore())
            .ForMember(d => d.ModifiedDateTime, o => o.Ignore())
            .ForMember(d => d.CreatedByPersonAliasId, o => o.Ignore())
            .ForMember(d => d.ModifiedByPersonAliasId, o => o.Ignore())
            .ForMember(d => d.GroupTypeId, o => o.Ignore()) // Set by service
            .ForMember(d => d.ParentGroupId, o => o.Ignore()) // Set by service
            .ForMember(d => d.CampusId, o => o.Ignore()) // Set by service
            .ForMember(d => d.IsSystem, o => o.MapFrom(s => false))
            .ForMember(d => d.IsArchived, o => o.MapFrom(s => false))
            .ForMember(d => d.IsSecurityRole, o => o.MapFrom(s => false))
            .ForMember(d => d.ArchivedByPersonAliasId, o => o.Ignore())
            .ForMember(d => d.ArchivedDateTime, o => o.Ignore())
            .ForMember(d => d.ScheduleId, o => o.Ignore())
            .ForMember(d => d.WelcomeSystemCommunicationId, o => o.Ignore())
            .ForMember(d => d.ExitSystemCommunicationId, o => o.Ignore())
            .ForMember(d => d.RequiredSignatureDocumentTemplateId, o => o.Ignore())
            .ForMember(d => d.StatusValueId, o => o.Ignore())
            .ForMember(d => d.GroupType, o => o.Ignore())
            .ForMember(d => d.Campus, o => o.Ignore())
            .ForMember(d => d.ParentGroup, o => o.Ignore())
            .ForMember(d => d.ChildGroups, o => o.Ignore())
            .ForMember(d => d.Members, o => o.Ignore());
    }
}
