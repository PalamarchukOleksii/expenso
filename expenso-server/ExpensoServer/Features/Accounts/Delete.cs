using System.Security.Claims;
using ExpensoServer.Common.Api;
using ExpensoServer.Common.Api.Extensions;
using ExpensoServer.Data;
using ExpensoServer.Data.Entities;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace ExpensoServer.Features.Accounts;

public static class Delete
{
    public class Endpoint : IEndpoint
    {
        public static void Map(IEndpointRouteBuilder app)
        {
            app.MapDelete("{id:guid}", HandleAsync);
        }
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        ApplicationDbContext dbContext,
        ClaimsPrincipal claimsPrincipal,
        CancellationToken cancellationToken)
    {
        var userId = claimsPrincipal.GetUserId();
        var account =
            await dbContext.Accounts.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId, cancellationToken);
        if (account == null)
            return TypedResults.NotFound();

        dbContext.Accounts.Remove(account);
        await dbContext.SaveChangesAsync(cancellationToken);

        return TypedResults.NoContent();
    }
}