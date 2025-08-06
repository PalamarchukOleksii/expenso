using System.Security.Claims;
using ExpensoServer.Common.Endpoints;
using ExpensoServer.Common.Endpoints.Constants;
using ExpensoServer.Common.Endpoints.Extensions;
using ExpensoServer.Common.Endpoints.Filters;
using ExpensoServer.Data;
using ExpensoServer.Data.Entities;
using ExpensoServer.Data.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ExpensoServer.Features.ExpenseCategories;

public static class Create
{
    public class Endpoint : IEndpoint
    {
        public static void Map(IEndpointRouteBuilder app)
        {
            app.MapPost("/create", HandleAsync)
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
                .MaximumLength(50).WithMessage("Name must be at most 50 characters long.");
        }
    }

    private static async Task<IResult> HandleAsync(
        Request request,
        ApplicationDbContext dbContext,
        ClaimsPrincipal claimsPrincipal,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var userId = claimsPrincipal.GetUserId();

        var nameExists = await dbContext.Categories.AnyAsync(c =>
            c.Name == request.Name &&
            c.Type == CategoryType.Expense &&
            (c.UserId == userId || c.IsDefault), cancellationToken);

        if (nameExists)
            return TypedResults.Conflict();

        var category = new Category
        {
            UserId = userId,
            Name = request.Name,
            Type = CategoryType.Expense
        };

        dbContext.Categories.Add(category);
        await dbContext.SaveChangesAsync(cancellationToken);

        var location =
            $"{httpContext.Request.Scheme}://{httpContext.Request.Host}/{Routes.Prefix}/{Routes.Segments.ExpenseCategories}/{category.Id}";

        return TypedResults.Created(location, new Response(category.Id, category.Name));
    }
}