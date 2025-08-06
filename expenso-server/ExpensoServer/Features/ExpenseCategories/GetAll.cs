using System.Security.Claims;
using ExpensoServer.Common.Endpoints;
using ExpensoServer.Common.Endpoints.Extensions;
using ExpensoServer.Data;
using ExpensoServer.Data.Enums;
using Microsoft.EntityFrameworkCore;

namespace ExpensoServer.Features.ExpenseCategories;

public static class GetAll
{
    public class Endpoint : IEndpoint
    {
        public static void Map(IEndpointRouteBuilder app)
        {
            app.MapGet("/", HandleAsync);
        }
    }

    public record Response(Guid Id, string Name);

    private static async Task<IResult> HandleAsync(
        ClaimsPrincipal claimsPrincipal,
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var userId = claimsPrincipal.GetUserId();

        var categories = await dbContext.Categories
            .Where(c => (c.UserId == userId || c.IsDefault) && c.Type == CategoryType.Expense)
            .Select(c => new Response(c.Id, c.Name))
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(categories);
    }
}