using System.Security.Claims;
using ExpensoServer.Common.Api;
using ExpensoServer.Common.Api.Extensions;
using ExpensoServer.Data;
using ExpensoServer.Data.Enums;
using Microsoft.AspNetCore.Server.IISIntegration;
using Microsoft.EntityFrameworkCore;

namespace ExpensoServer.Features.IncomeCategories;

public static class Delete
{
    public class Endpoint : IEndpoint
    {
        public static void Map(IEndpointRouteBuilder app)
        {
            app.MapDelete("/{id:guid}", HandleAsync);
        }
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        ApplicationDbContext dbContext,
        ClaimsPrincipal claimsPrincipal,
        CancellationToken cancellationToken)
    {
        if (await dbContext.Categories.AnyAsync(a => a.Id == id && a.IsDefault && a.Type == CategoryType.Income,
                cancellationToken))
            return TypedResults.Forbid();

        var userId = claimsPrincipal.GetUserId();

        var category =
            await dbContext.Categories.FirstOrDefaultAsync(
                a => a.Id == id && a.UserId == userId && a.Type == CategoryType.Income, cancellationToken);
        if (category == null)
            return TypedResults.NotFound();

        dbContext.Categories.Remove(category);
        await dbContext.SaveChangesAsync(cancellationToken);

        return TypedResults.NoContent();
    }
}