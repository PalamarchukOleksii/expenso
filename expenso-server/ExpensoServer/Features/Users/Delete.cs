using System.Security.Claims;
using ExpensoServer.Common.Endpoints;
using ExpensoServer.Common.Endpoints.Extensions;
using ExpensoServer.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace ExpensoServer.Features.Users;

public static class Delete
{
    public class Endpoint : IEndpoint
    {
        public static void Map(IEndpointRouteBuilder app)
        {
            app.MapDelete("/current", HandleAsync)
                .Produces(StatusCodes.Status204NoContent)
                .ProducesProblem(StatusCodes.Status404NotFound);
        }
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> HandleAsync(
        ApplicationDbContext dbContext,
        ClaimsPrincipal claimsPrincipal,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var userId = claimsPrincipal.GetUserId();

        var user = await dbContext.Users
            .Include(u => u.Accounts)
            .ThenInclude(a => a.FromOperations)
            .Include(u => u.Accounts)
            .ThenInclude(a => a.ToOperations)
            .Include(u => u.Categories)
            .ThenInclude(c => c.Operations)
            .Include(u => u.Operations)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
            return TypedResults.Problem(
                title: "User Not Found",
                detail: $"User with ID '{userId}' was not found.",
                statusCode: StatusCodes.Status404NotFound);

        dbContext.Users.Remove(user);
        await dbContext.SaveChangesAsync(cancellationToken);

        await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        return TypedResults.NoContent();
    }
}