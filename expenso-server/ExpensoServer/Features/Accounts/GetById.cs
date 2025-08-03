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
            app.MapGet("{id:guid}", HandleAsync);
        }
    }

    public record Response(Guid Id, Guid userId, string Name, decimal Balance, Currency Currency);

    private static async Task<Results<NotFound, Ok<Response>>> HandleAsync(Guid id, ApplicationDbContext dbContext,
        ClaimsPrincipal claimsPrincipal, CancellationToken cancellationToken)
    {
        var userId = claimsPrincipal.GetUserId();
        var account = await dbContext.GetAccountByUserIdAndAccountIdAsync(userId, id, cancellationToken);
        if (account == null)
            return TypedResults.NotFound();

        var response = new Response(account.Id, account.UserId, account.Name, account.Balance, account.Currency);
        return TypedResults.Ok(response);
    }

    private static async Task<Account?> GetAccountByUserIdAndAccountIdAsync(
        this ApplicationDbContext context,
        Guid userId,
        Guid accountId,
        CancellationToken cancellationToken)
    {
        return await context.Accounts.FirstOrDefaultAsync(a => a.Id == accountId && a.UserId == userId,
            cancellationToken);
    }
}