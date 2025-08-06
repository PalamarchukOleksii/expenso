using System.Security.Claims;
using ExpensoServer.Common.Endpoints;
using ExpensoServer.Common.Endpoints.Extensions;
using ExpensoServer.Data;
using Microsoft.EntityFrameworkCore;

namespace ExpensoServer.Features.Users;

public static class GetCurrent
{
    public class Endpoints : IEndpoint
    {
        public static void Map(IEndpointRouteBuilder app)
        {
            app.MapGet("/current", HandleAsync);
        }
    }

    public record Response(Guid Id, string Email);

    private static async Task<IResult> HandleAsync(
        ClaimsPrincipal claimsPrincipal,
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var userId = claimsPrincipal.GetUserId();

        var response = await dbContext.Users
            .Where(u => u.Id == userId)
            .Select(u => new Response(u.Id, u.Email))
            .FirstOrDefaultAsync(cancellationToken);

        return response is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(response);
    }
}