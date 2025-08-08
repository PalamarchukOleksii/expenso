using System.Security.Claims;
using ExpensoServer.Common.Abstractions;
using ExpensoServer.Common.Extensions;
using ExpensoServer.Data;
using ExpensoServer.Data.Enums;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace ExpensoServer.Features.IncomeCategories;

public static class Delete
{
    public class Endpoint : IEndpoint
    {
        public static void Map(IEndpointRouteBuilder app)
        {
            app.MapDelete("/{id:guid}", HandleAsync)
                .Produces(StatusCodes.Status204NoContent)
                .ProducesProblem(StatusCodes.Status404NotFound)
                .ProducesProblem(StatusCodes.Status403Forbidden);
        }
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> HandleAsync(
        Guid id,
        ApplicationDbContext dbContext,
        ClaimsPrincipal claimsPrincipal,
        CancellationToken cancellationToken)
    {
        var userId = claimsPrincipal.GetUserId();

        var category = await dbContext.Categories
            .Include(c => c.Operations)
            .Where(x => x.Type == CategoryType.Income)
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, cancellationToken);

        if (category is null)
            return TypedResults.Problem(
                title: "Income category Not Found",
                detail: $"Income category with ID '{id}' was not found for the current user.",
                statusCode: StatusCodes.Status404NotFound);

        if (category.IsDefault)
            return TypedResults.Problem(
                title: "Forbidden",
                detail: "Cannot delete the default income category.",
                statusCode: StatusCodes.Status403Forbidden);

        dbContext.Categories.Remove(category);
        await dbContext.SaveChangesAsync(cancellationToken);

        return TypedResults.NoContent();
    }
}