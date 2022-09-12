using System.Diagnostics;
using AuthUtils;

namespace API.Authorization;

public class PermissionsMiddleware
{
    private readonly RequestDelegate _next;

    public PermissionsMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context, IUserPermissionService permissionService)
    {
        if (context.User.Identity is not {IsAuthenticated: true})
        {
            await _next(context);
            return;
        }

        using var span = OTel.Tracer.StartActivity("PermissionMiddleware_Invoke");

        var cancellationToken = context.RequestAborted;

        var userSub = context.User.FindFirst(StandardJwtClaimTypes.Subject)?.Value;
        if (string.IsNullOrEmpty(userSub))
        {
            span?.SetStatus(ActivityStatusCode.Error);
            span?.AddEvent(new ActivityEvent("The'sub' claim is missing in the user identity"));

            await context.WriteAccessDeniedResponse("User 'sub' claim is required",
                cancellationToken: cancellationToken);
            return;
        }

        span?.SetTag("sub", userSub);
        var permissionsIdentity = await permissionService.GetUserPermissionsIdentity(userSub, cancellationToken);
        if (permissionsIdentity == null)
        {
            span?.SetStatus(ActivityStatusCode.Error);
            span?.AddEvent(new ActivityEvent("No permissions found for the user on the database"));

            await context.WriteAccessDeniedResponse(cancellationToken: cancellationToken);
            return;
        }

        // User has permissions, so we add the extra identity containing the "permissions" claims
        context.User.AddIdentity(permissionsIdentity);
        await _next(context);
    }
}
