using System.Security.Claims;
using ExpensoServer.Common.Endpoints;
using ExpensoServer.Common.Endpoints.Extensions;
using ExpensoServer.Common.Endpoints.Filters;
using ExpensoServer.Data;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace ExpensoServer.Features.Users;

public static class Update
{
    public class Endpoint : IEndpoint
    {
        public static void Map(IEndpointRouteBuilder app)
        {
            app.MapPatch("/current", HandleAsync)
                .WithRequestValidation<Request>()
                .Produces<Response>()
                .ProducesProblem(StatusCodes.Status404NotFound)
                .ProducesProblem(StatusCodes.Status409Conflict);
        }
    }

    public record Request(string Email);

    public record Response(Guid Id, string Email);

    public class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("Invalid email format.");
        }
    }

    private static async Task<Results<Ok<Response>, ProblemHttpResult>> HandleAsync(
        Request request,
        ApplicationDbContext dbContext,
        ClaimsPrincipal claimsPrincipal,
        CancellationToken cancellationToken)
    {
        var userId = claimsPrincipal.GetUserId();

        var user = await dbContext.Users
            .Where(u => u.Id == userId)
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null)
            return TypedResults.Problem(
                title: "User Not Found",
                detail: $"User with ID '{userId}' was not found.",
                statusCode: StatusCodes.Status404NotFound);

        if (user.Email == request.Email)
            return TypedResults.Ok(new Response(user.Id, user.Email));

        var isEmailConflict = await dbContext.Users.AnyAsync(u =>
            u.Email == request.Email &&
            u.Id != userId, cancellationToken);

        if (isEmailConflict)
            return TypedResults.Problem(
                title: "Email Conflict",
                detail: $"The email '{request.Email}' is already taken by another user.",
                statusCode: StatusCodes.Status409Conflict);

        user.Email = request.Email;
        await dbContext.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok(new Response(user.Id, user.Email));
    }
}