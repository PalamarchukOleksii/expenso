using System.Security.Claims;
using ExpensoServer.Common.Abstractions;
using ExpensoServer.Common.Extensions;
using ExpensoServer.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ExpensoServer.Features.Accounts;

public static class GetAll
{
    public class Endpoint : IEndpoint
    {
        public static void Map(IEndpointRouteBuilder app)
        {
            app.MapGet("/", HandleAsync)
                .Produces<Response[]>();
        }
    }

    public record Response(Guid Id, string Name, decimal Balance, string Currency);

    private static async Task<Ok<Response[]>> HandleAsync(
        ClaimsPrincipal claimsPrincipal,
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var userId = claimsPrincipal.GetUserId();

        var accounts = await dbContext.Accounts
            .Where(a => a.UserId == userId)
            .Select(a => new Response(a.Id, a.Name, a.Balance, a.Currency.ToString()))
            .ToArrayAsync(cancellationToken);

        return TypedResults.Ok(accounts);
    }
}