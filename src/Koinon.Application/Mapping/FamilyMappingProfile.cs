using AutoMapper;
using Koinon.Application.DTOs;
using Koinon.Application.DTOs.Requests;
using Koinon.Domain.Entities;

namespace Koinon.Application.Mapping;

/// <summary>
/// AutoMapper profile for Family-related entity mappings.
/// </summary>
public class FamilyMappingProfile : Profile
{
    public FamilyMappingProfile()
    {
        // Entity to DTO mappings
        CreateMap<Family, FamilyDto>()
            .ForMember(d => d.IdKey, o => o.MapFrom(s => s.IdKey))
            .ForMember(d => d.Description, o => o.MapFrom(s => (string?)null)) // Family entity doesn't have Description
            .ForMember(d => d.Address, o => o.Ignore()) // Will be set separately
            .ForMember(d => d.Members, o => o.Ignore()); // Will be set separately

        CreateMap<FamilyMember, FamilyMemberDto>()
            .ForMember(d => d.IdKey, o => o.MapFrom(s => s.IdKey))
            .ForMember(d => d.Person, o => o.MapFrom(s => s.Person))
            .ForMember(d => d.Role, o => o.MapFrom(s => s.FamilyRole))
            .ForMember(d => d.Status, o => o.MapFrom(s => "Active")) // FamilyMember doesn't have status
            .ForMember(d => d.DateTimeAdded, o => o.MapFrom(s => (DateTime?)s.DateAdded));

        CreateMap<Location, AddressDto>()
            .ForMember(d => d.IdKey, o => o.MapFrom(s => s.IdKey))
            .ForMember(d => d.FormattedAddress, o => o.MapFrom(s => FormatAddress(s)));

        CreateMap<GroupTypeRole, GroupTypeRoleDto>()
            .ForMember(d => d.IdKey, o => o.MapFrom(s => s.IdKey))
            .ForMember(d => d.Name, o => o.MapFrom(s => s.Name))
            .ForMember(d => d.IsLeader, o => o.MapFrom(s => s.IsLeader));

        // Request to Entity mappings
        CreateMap<CreateFamilyRequest, Family>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.Guid, o => o.Ignore())
            .ForMember(d => d.CreatedDateTime, o => o.Ignore())
            .ForMember(d => d.ModifiedDateTime, o => o.Ignore())
            .ForMember(d => d.CampusId, o => o.Ignore()) // Set by service
            .ForMember(d => d.IsActive, o => o.MapFrom(s => true))
            .ForMember(d => d.Campus, o => o.Ignore())
            .ForMember(d => d.Members, o => o.Ignore());
        // Note: Description in request is ignored (Family entity doesn't have this property)

        CreateMap<CreateFamilyAddressRequest, Location>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.Guid, o => o.Ignore())
            .ForMember(d => d.CreatedDateTime, o => o.Ignore())
            .ForMember(d => d.ModifiedDateTime, o => o.Ignore())
            .ForMember(d => d.CreatedByPersonAliasId, o => o.Ignore())
            .ForMember(d => d.ModifiedByPersonAliasId, o => o.Ignore())
            .ForMember(d => d.Name, o => o.MapFrom(s => "Home"))
            .ForMember(d => d.ParentLocationId, o => o.Ignore())
            .ForMember(d => d.LocationTypeValueId, o => o.Ignore())
            .ForMember(d => d.IsActive, o => o.MapFrom(s => true))
            .ForMember(d => d.Description, o => o.Ignore())
            .ForMember(d => d.PrinterDeviceId, o => o.Ignore())
            .ForMember(d => d.ImageId, o => o.Ignore())
            .ForMember(d => d.SoftRoomThreshold, o => o.Ignore())
            .ForMember(d => d.FirmRoomThreshold, o => o.Ignore())
            .ForMember(d => d.IsGeoPointLocked, o => o.Ignore())
            .ForMember(d => d.Latitude, o => o.Ignore())
            .ForMember(d => d.Longitude, o => o.Ignore())
            .ForMember(d => d.Order, o => o.MapFrom(s => 0))
            .ForMember(d => d.ParentLocation, o => o.Ignore())
            .ForMember(d => d.ChildLocations, o => o.Ignore())
            .ForMember(d => d.LocationTypeValue, o => o.Ignore())
            .ForMember(d => d.Country, o => o.MapFrom(s => s.Country ?? "USA"));
    }

    private static string FormatAddress(Location location)
    {
        if (location == null)
        {
            return string.Empty;
        }

        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(location.Street1))
        {
            parts.Add(location.Street1);
        }

        if (!string.IsNullOrWhiteSpace(location.Street2))
        {
            parts.Add(location.Street2);
        }

        var cityStateZip = new List<string>();
        if (!string.IsNullOrWhiteSpace(location.City))
        {
            cityStateZip.Add(location.City);
        }

        if (!string.IsNullOrWhiteSpace(location.State))
        {
            cityStateZip.Add(location.State);
        }

        if (!string.IsNullOrWhiteSpace(location.PostalCode))
        {
            cityStateZip.Add(location.PostalCode);
        }

        if (cityStateZip.Any())
        {
            parts.Add(string.Join(", ", cityStateZip));
        }

        if (!string.IsNullOrWhiteSpace(location.Country) && location.Country != "USA")
        {
            parts.Add(location.Country);
        }

        return string.Join("\n", parts);
    }
}
