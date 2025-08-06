using System.Security.Claims;
using ExpensoServer.Common.Endpoints;
using ExpensoServer.Common.Endpoints.Extensions;
using ExpensoServer.Common.Endpoints.Filters;
using ExpensoServer.Data;
using ExpensoServer.Data.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ExpensoServer.Features.ExpenseCategories;

public static class Update
{
    public class Endpoint : IEndpoint
    {
        public static void Map(IEndpointRouteBuilder app)
        {
            app.MapPatch("/{id:guid}", HandleAsync)
                .AddEndpointFilter<RequestValidationFilter<Request>>();
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

    private static async Task<IResult> HandleAsync(
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
            return TypedResults.NotFound();

        if (category.IsDefault)
            return TypedResults.Forbid();

        if (request.Name == category.Name)
            return TypedResults.Ok(new Response(category.Id, category.Name));

        var isNameConflict = await dbContext.Categories.AnyAsync(x =>
            x.Type == CategoryType.Expense &&
            x.Name == request.Name &&
            x.Id != id &&
            (x.UserId == userId || x.IsDefault), cancellationToken);

        if (isNameConflict)
            return TypedResults.Conflict();

        category.Name = request.Name;
        await dbContext.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok(new Response(category.Id, category.Name));
    }
}