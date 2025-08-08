using System.Security.Claims;
using ExpensoServer.Common.Endpoints;
using ExpensoServer.Common.Endpoints.Extensions;
using ExpensoServer.Data;
using ExpensoServer.Data.Enums;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace ExpensoServer.Features.IncomeCategories;

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

    public record Request(string Name);

    public record Response(Guid Id, string Name);

    public class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required.")
                .MinimumLength(3).WithMessage("Name must be at least 3 characters long.")
                .MaximumLength(100).WithMessage("Name must be at most 100 characters long.");
        }
    }

    private static async Task<Results<Ok<Response>, ProblemHttpResult>> HandleAsync(
        Guid id,
        Request request,
        ApplicationDbContext dbContext,
        ClaimsPrincipal claimsPrincipal,
        CancellationToken cancellationToken)
    {
        var userId = claimsPrincipal.GetUserId();

        var category = await dbContext.Categories
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
                detail: "Cannot update the default income category.",
                statusCode: StatusCodes.Status403Forbidden);

        if (request.Name == category.Name) return TypedResults.Ok(new Response(category.Id, category.Name));

        var isNameConflict = await dbContext.Categories.AnyAsync(x =>
            x.Type == CategoryType.Income &&
            x.Name == request.Name &&
            x.Id != id &&
            (x.UserId == userId || x.IsDefault), cancellationToken);

        if (isNameConflict)
            return TypedResults.Problem(
                title: "Conflict",
                detail: $"An income category with the name '{request.Name}' already exists.",
                statusCode: StatusCodes.Status409Conflict);

        category.Name = request.Name;
        await dbContext.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok(new Response(category.Id, category.Name));
    }
}