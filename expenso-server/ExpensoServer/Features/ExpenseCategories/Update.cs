using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using ExpensoServer.Common.Abstractions;
using ExpensoServer.Common.Extensions;
using ExpensoServer.Data;
using ExpensoServer.Data.Enums;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace ExpensoServer.Features.ExpenseCategories;

public static class Update
{
    public class Endpoint : IEndpoint
    {
        public static void Map(IEndpointRouteBuilder app)
        {
            app.MapPatch("/{id:guid}", HandleAsync)
                .WithRequestValidation<Request>()
                .Produces<Response>()
                .ProducesProblem(StatusCodes.Status404NotFound)
                .ProducesProblem(StatusCodes.Status403Forbidden)
                .ProducesProblem(StatusCodes.Status409Conflict);
        }
    }

    public class Request
    {
        [Required(ErrorMessage = "Name is required.")]
        [MinLength(3, ErrorMessage = "Name must be at least 3 characters long.")]
        [MaxLength(100, ErrorMessage = "Name must be at most 100 characters long.")]
        public string Name { get; set; } = default!;
    }

    public record Response(Guid Id, string Name);

    private static async Task<Results<Ok<Response>, ProblemHttpResult>> HandleAsync(
        Guid id,
        Request request,
        ApplicationDbContext dbContext,
        ClaimsPrincipal claimsPrincipal,
        CancellationToken cancellationToken)
    {
        var userId = claimsPrincipal.GetUserId();

        var category = await dbContext.Categories
            .Where(x => x.Type == CategoryType.Expense)
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, cancellationToken);

        if (category == null)
            return TypedResults.Problem(
                title: "Not Found",
                detail: $"Expense category with ID '{id}' was not found.",
                statusCode: StatusCodes.Status404NotFound);

        if (category.IsDefault)
            return TypedResults.Problem(
                title: "Forbidden",
                detail: "Cannot update the default expense category.",
                statusCode: StatusCodes.Status403Forbidden);

        if (request.Name == category.Name) return TypedResults.Ok(new Response(category.Id, category.Name));

        var isNameConflict = await dbContext.Categories.AnyAsync(x =>
            x.Type == CategoryType.Expense &&
            x.Name == request.Name &&
            x.Id != id &&
            (x.UserId == userId || x.IsDefault), cancellationToken);

        if (isNameConflict)
            return TypedResults.Problem(
                title: "Conflict",
                detail: $"An expense category with the name '{request.Name}' already exists.",
                statusCode: StatusCodes.Status409Conflict);

        category.Name = request.Name;
        await dbContext.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok(new Response(category.Id, category.Name));
    }
}