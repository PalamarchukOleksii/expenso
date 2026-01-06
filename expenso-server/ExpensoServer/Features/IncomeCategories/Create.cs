using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using ExpensoServer.Common.Abstractions;
using ExpensoServer.Common.Constants;
using ExpensoServer.Common.Extensions;
using ExpensoServer.Data;
using ExpensoServer.Data.Entities;
using ExpensoServer.Data.Enums;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace ExpensoServer.Features.IncomeCategories;

public static class Create
{
    public class Endpoint : IEndpoint
    {
        public static void Map(IEndpointRouteBuilder app)
        {
            app.MapPost("/create", HandleAsync)
                .ProducesValidationProblem()
                .Produces<Response>(StatusCodes.Status201Created)
                .ProducesProblem(StatusCodes.Status409Conflict);
        }
    }

    public class Request
    {
        [Required(ErrorMessage = "Name is required.")]
        [MinLength(3, ErrorMessage = "Name must be at least 3 characters long.")]
        [MaxLength(50, ErrorMessage = "Name must be at most 50 characters long.")]
        public string Name { get; set; } = default!;
    }

    public record Response(Guid Id, string Name);

    private static async Task<Results<Created<Response>, ProblemHttpResult>> HandleAsync(
        Request request,
        ApplicationDbContext dbContext,
        ClaimsPrincipal claimsPrincipal,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var userId = claimsPrincipal.GetUserId();

        var nameExists = await dbContext.Categories.AnyAsync(c =>
            c.Name == request.Name &&
            c.Type == CategoryType.Income &&
            (c.UserId == userId || c.IsDefault), cancellationToken);

        if (nameExists)
            return TypedResults.Problem(
                title: "Conflict",
                detail: $"An income category with the name '{request.Name}' already exists.",
                statusCode: StatusCodes.Status409Conflict);

        var category = new Category
        {
            UserId = userId,
            Name = request.Name,
            Type = CategoryType.Income
        };

        dbContext.Categories.Add(category);
        await dbContext.SaveChangesAsync(cancellationToken);

        var location =
            $"{httpContext.Request.Scheme}://{httpContext.Request.Host}/{EndpointRoutes.Prefix}/{EndpointRoutes.Segments.IncomeCategories}/{category.Id}";

        return TypedResults.Created(location, new Response(category.Id, category.Name));
    }
}