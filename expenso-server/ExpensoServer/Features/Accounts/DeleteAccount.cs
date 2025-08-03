using System.Security.Claims;
using ExpensoServer.Common.Api;
using ExpensoServer.Common.Api.Extensions;
using ExpensoServer.Data;
using ExpensoServer.Data.Entities;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace ExpensoServer.Features.Accounts;

public static class DeleteAccount
{
    public class Endpoint : IEndpoint
    {
        public static void Map(IEndpointRouteBuilder app)
        {
            app.MapDelete("{id:guid}", HandleAsync)
                .Produces(StatusCodes.Status204NoContent)
                .ProducesProblem(StatusCodes.Status404NotFound);
        }
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> HandleAsync(
        Guid id,
        ApplicationDbContext dbContext,
        ClaimsPrincipal claimsPrincipal,
        CancellationToken cancellationToken)
    {
        var userId = claimsPrincipal.GetUserId();
        var account = await dbContext.GetAccountByUserIdAndAccountIdAsync(userId, id, cancellationToken);
        if (account == null)
            return TypedResults.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "NotFound",
                detail: $"No account found with ID '{id}' for the current user.",
                type: "https://tools.ietf.org/html/rfc7231#section-6.5.4"
            );

        dbContext.Accounts.Remove(account);
        await dbContext.SaveChangesAsync(cancellationToken);

        return TypedResults.NoContent();
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