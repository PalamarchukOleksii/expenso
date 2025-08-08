using System.Security.Claims;
using ExpensoServer.Common.Endpoints;
using ExpensoServer.Common.Endpoints.Extensions;
using ExpensoServer.Data;
using ExpensoServer.Data.Enums;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace ExpensoServer.Features.ExpenseCategories;

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

    public record Response(Guid Id, string Name);

    private static async Task<Ok<Response[]>> HandleAsync(
        ClaimsPrincipal claimsPrincipal,
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var userId = claimsPrincipal.GetUserId();

        var categories = await dbContext.Categories
            .Where(c => (c.UserId == userId || c.IsDefault) && c.Type == CategoryType.Expense)
            .Select(c => new Response(c.Id, c.Name))
            .ToArrayAsync(cancellationToken);

        return TypedResults.Ok(categories);
    }
}