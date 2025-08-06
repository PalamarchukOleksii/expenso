using System.Security.Claims;
using ExpensoServer.Common.Endpoints;
using ExpensoServer.Common.Endpoints.Extensions;
using ExpensoServer.Data;
using ExpensoServer.Data.Enums;
using Microsoft.EntityFrameworkCore;

namespace ExpensoServer.Features.Accounts;

public static class GetAll
{
    public class Endpoint : IEndpoint
    {
        public static void Map(IEndpointRouteBuilder app)
        {
            app.MapGet("/", HandleAsync);
        }
    }

    public record Response(Guid Id, string Name, decimal Balance, string Currency);

    private static async Task<IResult> HandleAsync(ClaimsPrincipal claimsPrincipal, ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var userId = claimsPrincipal.GetUserId();

        var accounts = await dbContext.Accounts
            .Where(a => a.UserId == userId)
            .Select(a => new Response(a.Id, a.Name, a.Balance, a.Currency.ToString()))
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(accounts);
    }
}