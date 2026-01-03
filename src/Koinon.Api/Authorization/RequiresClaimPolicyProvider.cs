using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Koinon.Api.Authorization;

public class RequiresClaimPolicyProvider : IAuthorizationPolicyProvider
{
    private const string PolicyPrefix = "permission:";
    private readonly DefaultAuthorizationPolicyProvider _fallbackProvider;

    public RequiresClaimPolicyProvider(IOptions<AuthorizationOptions> options)
    {
        _fallbackProvider = new DefaultAuthorizationPolicyProvider(options);
    }

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (policyName.StartsWith(PolicyPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var permission = policyName.Substring(PolicyPrefix.Length);
            var parts = permission.Split('.', 2);
            var claimType = parts[0];
            var claimValue = parts.Length > 1 ? parts[1] : string.Empty;

            var policy = new AuthorizationPolicyBuilder()
                .AddRequirements(new RequiresClaimRequirement(claimType, claimValue))
                .Build();
            return Task.FromResult<AuthorizationPolicy?>(policy);
        }

        return _fallbackProvider.GetPolicyAsync(policyName);
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() =>
        _fallbackProvider.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() =>
        _fallbackProvider.GetFallbackPolicyAsync();
}
