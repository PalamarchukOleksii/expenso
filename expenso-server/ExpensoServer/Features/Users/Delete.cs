using System.Security.Claims;
using ExpensoServer.Common.Endpoints;
using ExpensoServer.Common.Endpoints.Extensions;
using ExpensoServer.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

namespace ExpensoServer.Features.Users;

public static class Delete
{
    public class Endpoint : IEndpoint
    {
        public static void Map(IEndpointRouteBuilder app)
        {
            app.MapDelete("/current", HandleAsync);
        }
    }

    private static async Task<IResult> HandleAsync(ApplicationDbContext dbContext, ClaimsPrincipal claimsPrincipal,
        HttpContext httpContext, CancellationToken cancellationToken)
    {
        var userId = claimsPrincipal.GetUserId();
        var user = await dbContext.Users
            .FirstOrDefaultAsync(a => a.Id == userId, cancellationToken);

        if (user == null)
            return TypedResults.NotFound();

        dbContext.Users.Remove(user);
        await dbContext.SaveChangesAsync(cancellationToken);
        
        await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        return TypedResults.NoContent();
    }
}