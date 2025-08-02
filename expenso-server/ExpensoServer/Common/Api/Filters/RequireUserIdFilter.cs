using System.Security.Claims;
using ExpensoServer.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

namespace ExpensoServer.Common.Api.Filters;

public class RequireUserIdFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var userIdClaim = context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Authentication Required",
                detail: "User must be authenticated to access this resource.",
                type: "https://tools.ietf.org/html/rfc7235#section-3.1"
            );
        }
        
        var dbContext = context.HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
        
        var userExists = await dbContext.Users.AnyAsync(u => u.Id == userId);
        if (userExists) return await next(context);
        
        await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            
        return TypedResults.Problem(
            statusCode: StatusCodes.Status401Unauthorized,
            title: "Invalid User",
            detail: "The authenticated user does not exist in the system.",
            type: "https://tools.ietf.org/html/rfc7235#section-3.1"
        );

    }
}
