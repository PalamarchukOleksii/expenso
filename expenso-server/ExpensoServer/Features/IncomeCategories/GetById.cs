using System.Security.Claims;
using ExpensoServer.Common.Api;
using ExpensoServer.Common.Api.Extensions;
using ExpensoServer.Data;
using ExpensoServer.Data.Enums;
using Microsoft.EntityFrameworkCore;

namespace ExpensoServer.Features.IncomeCategories;

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

    private static async Task<IResult> HandleAsync(Guid id,
        ApplicationDbContext dbContext,
        ClaimsPrincipal claimsPrincipal, CancellationToken cancellationToken)
    {
        var userId = claimsPrincipal.GetUserId();
        var category =
            await dbContext.Categories.FirstOrDefaultAsync(
                a => a.Id == id && (a.UserId == userId || a.IsDefault) && a.Type == CategoryType.Income,
                cancellationToken);
        if (category == null)
            return TypedResults.NotFound();

        return TypedResults.Ok(
            new Response(category.Id, category.Name));
    }
}