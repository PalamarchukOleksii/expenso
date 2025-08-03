using System.Security.Claims;
using ExpensoServer.Common.Api;
using ExpensoServer.Common.Api.Constants;
using ExpensoServer.Common.Api.Extensions;
using ExpensoServer.Data;
using ExpensoServer.Data.Entities;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace ExpensoServer.Features.IncomingCategories;

public static class CreateIncomingCategory
{
    public class Endpoint : IEndpoint
    {
        public static void Map(IEndpointRouteBuilder app)
        {
            app.MapPost("/create", HandleAsync)
                .WithRequestValidation<Request>()
                .ProducesProblem(StatusCodes.Status409Conflict)
                .Produces<Response>();
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

    private static async Task<Results<Created<Response>, ProblemHttpResult>> HandleAsync(
        Request request,
        ApplicationDbContext dbContext,
        ClaimsPrincipal claimsPrincipal,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (await dbContext.IsDefaultIncomingCategoryExistByNameAsync(request.Name, cancellationToken))
            return TypedResults.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Conflict",
                detail: $"An default incoming category with the name '{request.Name}' already exists.",
                type: "https://tools.ietf.org/html/rfc7231#section-6.5.8"
            );

        var userId = claimsPrincipal.GetUserId();

        if (await dbContext.IsIncomingCategoryExistByUserIdAndNameAsync(userId, request.Name, cancellationToken))
            return TypedResults.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Conflict",
                detail: $"An incoming category with the name '{request.Name}' already exists for this user.",
                type: "https://tools.ietf.org/html/rfc7231#section-6.5.8"
            );

        var incomingCategory = new IncomingCategory
        {
            UserId = userId,
            Name = request.Name
        };

        dbContext.IncomingCategories.Add(incomingCategory);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = new Response(incomingCategory.Id, incomingCategory.Name);
        var location = httpContext.GetCreatedIncomingCategoryLocation(incomingCategory.Id);
        return TypedResults.Created(location, response);
    }

    private static async Task<bool> IsIncomingCategoryExistByUserIdAndNameAsync(
        this ApplicationDbContext dbContext,
        Guid userId,
        string incomingCategoryName,
        CancellationToken cancellationToken)
    {
        return await dbContext.IncomingCategories
            .AnyAsync(x => x.UserId == userId && x.Name == incomingCategoryName, cancellationToken);
    }

    private static async Task<bool> IsDefaultIncomingCategoryExistByNameAsync(this ApplicationDbContext dbContext,
        string name,
        CancellationToken cancellationToken)
    {
        return await dbContext.IncomingCategories.AnyAsync(u => u.Name == name && u.IsDefault, cancellationToken);
    }

    private static string GetCreatedIncomingCategoryLocation(this HttpContext httpContext, Guid incomingCategoryId)
    {
        return
            $"{httpContext.Request.Scheme}://{httpContext.Request.Host}/{ApiRoutes.Prefix}/{ApiRoutes.Segments.IncomingCategories}/{incomingCategoryId}";
    }
}