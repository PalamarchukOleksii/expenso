using System.Security.Claims;
using ExpensoServer.Common.Abstractions;
using ExpensoServer.Common.Extensions;
using ExpensoServer.Data;
using ExpensoServer.Data.Enums;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace ExpensoServer.Features.ExpenseCategories;

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
            .Where(x => x.Type == CategoryType.Expense)
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, cancellationToken);

        if (category == null)
            return TypedResults.Problem(
                title: "Not Found",
                detail: $"Expense category with id '{id}' was not found.",
                statusCode: StatusCodes.Status404NotFound);

        if (category.IsDefault)
            return TypedResults.Problem(
                title: "Forbidden",
                detail: "Cannot delete the default expense category.",
                statusCode: StatusCodes.Status403Forbidden);

        dbContext.Categories.Remove(category);

        await dbContext.SaveChangesAsync(cancellationToken);

        return TypedResults.NoContent();
    }
}