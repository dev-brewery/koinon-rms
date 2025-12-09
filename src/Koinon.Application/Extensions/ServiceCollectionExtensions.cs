using FluentValidation;
using Koinon.Application.DTOs.Auth;
using Koinon.Application.Interfaces;
using Koinon.Application.Services;
using Koinon.Application.Services.Common;
using Koinon.Application.Validators;
using Microsoft.Extensions.DependencyInjection;

namespace Koinon.Application.Extensions;

/// <summary>
/// Extension methods for IServiceCollection to configure Koinon application services.
/// Provides dependency injection registration for application-layer services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds all Koinon application services to the service collection.
    /// Registers services for person management, groups, families, and check-in operations.
    /// NOTE: IUserContext must be registered by the API/Infrastructure layer before calling this method.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddKoinonApplicationServices(
        this IServiceCollection services)
    {
        // NOTE: IUserContext is required by services but must be registered by the calling layer
        // (API or Infrastructure) before this method is called. This is intentional to keep
        // the Application layer independent of HTTP context or authentication implementation.

        // Authentication service
        services.AddScoped<IAuthService, AuthService>();

        // Core entity services
        services.AddScoped<IPersonService, PersonService>();
        services.AddScoped<IGroupService, GroupService>();
        services.AddScoped<IGroupTypeService, GroupTypeService>();
        services.AddScoped<IGroupMemberRequestService, GroupMemberRequestService>();
        services.AddScoped<IMyGroupsService, MyGroupsService>();
        services.AddScoped<IFamilyService, FamilyService>();
        services.AddScoped<IScheduleService, ScheduleService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IPublicGroupService, PublicGroupService>();

        // Check-in common services (foundation classes for consistent patterns)
        services.AddScoped<CheckinDataLoader>();
        services.AddScoped<ConcurrentOperationHelper>();

        // Grade calculation service
        services.AddScoped<IGradeCalculationService, GradeCalculationService>();

        // Check-in services
        services.AddScoped<ICheckinConfigurationService, CheckinConfigurationService>();
        services.AddScoped<ICheckinSearchService, CheckinSearchService>();
        services.AddScoped<ICheckinAttendanceService, CheckinAttendanceService>();
        services.AddScoped<ILabelGenerationService, LabelGenerationService>();
        services.AddScoped<ICapacityService, CapacityService>();

        // Device/Kiosk validation service
        services.AddScoped<IDeviceValidationService, DeviceValidationService>();

        // Supervisor mode service
        services.AddScoped<ISupervisorModeService, SupervisorModeService>();

        // Analytics services
        services.AddScoped<IAttendanceAnalyticsService, AttendanceAnalyticsService>();

        // First-time visitor and follow-up services
        services.AddScoped<IFirstTimeVisitorService, FirstTimeVisitorService>();
        services.AddScoped<IFollowUpService, FollowUpService>();

        // Parent paging service
        services.AddScoped<IParentPagingService, ParentPagingService>();

        // Authorized pickup service
        services.AddScoped<IAuthorizedPickupService, AuthorizedPickupService>();

        // Room roster service
        services.AddScoped<IRoomRosterService, RoomRosterService>();

        // Communication services
        services.AddScoped<ICommunicationService, CommunicationService>();
        services.AddScoped<ICommunicationSender, CommunicationSender>();

        // Self-service profile service
        services.AddScoped<IMyProfileService, MyProfileService>();

        // File management service
        services.AddScoped<IFileService, FileService>();

        // AutoMapper - register all mapping profiles
        services.AddAutoMapper(typeof(ServiceCollectionExtensions).Assembly);

        // FluentValidation - register all validators
        services.AddValidatorsFromAssembly(typeof(ServiceCollectionExtensions).Assembly);

        // Future: Add MediatR when needed
        // services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(ServiceCollectionExtensions).Assembly));

        return services;
    }
}
