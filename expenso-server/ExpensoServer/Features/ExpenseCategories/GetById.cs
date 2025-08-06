using System.Security.Claims;
using ExpensoServer.Common.Endpoints;
using ExpensoServer.Common.Endpoints.Extensions;
using ExpensoServer.Data;
using ExpensoServer.Data.Enums;
using Microsoft.EntityFrameworkCore;

namespace ExpensoServer.Features.ExpenseCategories;

public static class GetById
{
    public class Endpoint : IEndpoint
    {
        public static void Map(IEndpointRouteBuilder app)
        {
            app.MapGet("/{id:guid}", HandleAsync);
        }
    }

    public record Response(Guid Id, string Name);

    private static async Task<IResult> HandleAsync(
        Guid id,
        ApplicationDbContext dbContext,
        ClaimsPrincipal claimsPrincipal,
        CancellationToken cancellationToken)
    {
        var userId = claimsPrincipal.GetUserId();

        var category = await dbContext.Categories
            .Where(c => (c.UserId == userId || c.IsDefault) && c.Type == CategoryType.Expense)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (category == null)
            return TypedResults.NotFound();

        return TypedResults.Ok(new Response(category.Id, category.Name));
    }
}