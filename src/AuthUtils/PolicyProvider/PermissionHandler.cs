using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.AspNetCore.Authorization;

namespace AuthUtils.PolicyProvider;

public class PermissionHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly Counter<long> _authorizationsCounter = OTel.Meter.CreateCounter<long>("api.authorizations");
    private readonly KeyValuePair<string, object?> _authorizedAttribute = new("authorization.result", "authorized");
    private readonly KeyValuePair<string, object?> _unauthorizedAttribute = new("authorization.result", "unauthorized");

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        using var span = OTel.Tracer.StartActivity("HandleRequirementAsync");
        span?.SetTag("RequirementOperator", requirement.PermissionOperator);

        if (requirement.PermissionOperator == PermissionOperator.And)
        {
            foreach (var permission in requirement.Permissions)
            {
                if (!context.User.HasClaim(PermissionRequirement.ClaimType, permission))
                {
                    context.Fail();
                    span?.AddEvent(
                        new ActivityEvent("The user does not have all the required permissions",
                            DateTimeOffset.UtcNow,
                            new ActivityTagsCollection(new[]
                            {
                                new KeyValuePair<string, object?>("MissingPermission", permission)
                            })));
                    span?.SetStatus(ActivityStatusCode.Error);
                    
                    // Record the number of unauthorized requests
                    _authorizationsCounter.Add(1, _unauthorizedAttribute);
                    
                    return Task.CompletedTask;
                }
            }

            // identity has all required permissions
            context.Succeed(requirement);
            span?.AddEvent(new ActivityEvent("The user has the required permissions"));
            span?.SetStatus(ActivityStatusCode.Ok);
            
            // Record the number of authorized requests
            _authorizationsCounter.Add(1, _authorizedAttribute);
            
            return Task.CompletedTask;
        }

        foreach (var permission in requirement.Permissions)
        {
            if (context.User.HasClaim(PermissionRequirement.ClaimType, permission))
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }
        }

        // identity does not have any of the required permissions
        context.Fail();
        span?.AddEvent(
            new ActivityEvent("The user does not have any of the required permissions",
                DateTimeOffset.UtcNow,
                new ActivityTagsCollection(new[]
                {
                    new KeyValuePair<string, object?>(
                        "RequiredPermissions", string.Join(',', requirement.Permissions))
                })));
        span?.SetStatus(ActivityStatusCode.Error);
        
        // Record the number of unauthorized requests
        _authorizationsCounter.Add(1, _unauthorizedAttribute);
        
        return Task.CompletedTask;
    }
}
