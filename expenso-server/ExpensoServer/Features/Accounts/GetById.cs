using System.Security.Claims;
using ExpensoServer.Common.Api;
using ExpensoServer.Common.Api.Extensions;
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
        var account =
            await dbContext.Accounts.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId, cancellationToken);
        if (account == null)
            return TypedResults.NotFound();

        return TypedResults.Ok(
            new Response(account.Id, account.Name, account.Balance, account.Currency.ToString()));
    }
}