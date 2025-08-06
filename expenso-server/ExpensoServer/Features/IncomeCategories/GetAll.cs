using System.Security.Claims;
using ExpensoServer.Common.Api;
using ExpensoServer.Common.Api.Extensions;
using ExpensoServer.Data;
using ExpensoServer.Data.Enums;
using Microsoft.EntityFrameworkCore;

namespace ExpensoServer.Features.IncomeCategories;

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

    private static async Task<IResult> HandleAsync(ClaimsPrincipal claimsPrincipal, ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var userId = claimsPrincipal.GetUserId();

        var categories = await dbContext.Categories
            .Where(a => (a.UserId == userId || a.IsDefault) && a.Type == CategoryType.Income)
            .Select(a => new Response(
                a.Id,
                a.Name
            ))
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(categories);
    }
}