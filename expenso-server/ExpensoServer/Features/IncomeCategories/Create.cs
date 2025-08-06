using System.Security.Claims;
using ExpensoServer.Common.Api;
using ExpensoServer.Common.Api.Constants;
using ExpensoServer.Common.Api.Extensions;
using ExpensoServer.Common.Api.Filters;
using ExpensoServer.Data;
using ExpensoServer.Data.Entities;
using ExpensoServer.Data.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ExpensoServer.Features.IncomeCategories;

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

    private static async Task<IResult> HandleAsync(Request request, ApplicationDbContext dbContext,
        ClaimsPrincipal claimsPrincipal, HttpContext httpContext, CancellationToken cancellationToken)
    {
        var userId = claimsPrincipal.GetUserId();

        if (await dbContext.Categories.AnyAsync(
                x => x.Name == request.Name && x.IsDefault && x.Type == CategoryType.Income, cancellationToken) ||
            await dbContext.Categories.AnyAsync(
                x => x.UserId == userId && x.Name == request.Name && x.Type == CategoryType.Income, cancellationToken))
            return TypedResults.Conflict();

        var category = new Category
        {
            UserId = userId,
            Name = request.Name,
            Type = CategoryType.Income
        };

        dbContext.Categories.Add(category);
        await dbContext.SaveChangesAsync(cancellationToken);

        return TypedResults.Created(
            $"{httpContext.Request.Scheme}://{httpContext.Request.Host}/{ApiRoutes.Prefix}/{ApiRoutes.Segments.IncomeCategories}/{category.Id}",
            new Response(category.Id, category.Name));
    }
}