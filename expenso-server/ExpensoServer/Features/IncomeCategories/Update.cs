using System.Security.Claims;
using ExpensoServer.Common.Api;
using ExpensoServer.Common.Api.Extensions;
using ExpensoServer.Common.Api.Filters;
using ExpensoServer.Data;
using ExpensoServer.Data.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ExpensoServer.Features.IncomeCategories;

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
        if (await dbContext.Categories.AnyAsync(a => a.Id == id && a.IsDefault, cancellationToken))
            return TypedResults.Forbid();

        var userId = claimsPrincipal.GetUserId();

        var category =
            await dbContext.Categories.FirstOrDefaultAsync(
                a => a.Id == id && a.UserId == userId && a.Type == CategoryType.Income, cancellationToken);
        if (category == null)
            return TypedResults.NotFound();


        if (request.Name != category.Name &&
            await dbContext.Categories.AnyAsync(
                x => (x.UserId == userId || x.IsDefault) && x.Name == request.Name && x.Type == CategoryType.Income,
                cancellationToken))
            return TypedResults.Conflict();

        category.Name = request.Name;
        await dbContext.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok(new Response(category.Id, category.Name));
    }
}