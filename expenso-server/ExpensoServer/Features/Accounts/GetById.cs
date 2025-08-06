using System.Security.Claims;
using ExpensoServer.Common.Endpoints;
using ExpensoServer.Common.Endpoints.Extensions;
using ExpensoServer.Data;
using ExpensoServer.Data.Entities;
using ExpensoServer.Data.Enums;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace ExpensoServer.Features.Accounts;

public static class GetById
{
    public class Endpoint : IEndpoint
    {
        public static void Map(IEndpointRouteBuilder app)
        {
            app.MapGet("/{id:guid}", HandleAsync);
        }
    }

    public record Response(Guid Id, string Name, decimal Balance, string Currency);

    private static async Task<IResult> HandleAsync(Guid id,
        ApplicationDbContext dbContext,
        ClaimsPrincipal claimsPrincipal, CancellationToken cancellationToken)
    {
        var userId = claimsPrincipal.GetUserId();
        var response = await dbContext.Accounts
            .Where(a => a.UserId == userId)
            .Select(a => new Response(a.Id, a.Name, a.Balance, a.Currency.ToString()))
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        return response is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(response);
    }
}