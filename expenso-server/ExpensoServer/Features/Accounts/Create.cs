using System.Security.Claims;
using ExpensoServer.Common.Endpoints;
using ExpensoServer.Common.Endpoints.Constants;
using ExpensoServer.Common.Endpoints.Extensions;
using ExpensoServer.Data;
using ExpensoServer.Data.Entities;
using ExpensoServer.Data.Enums;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace ExpensoServer.Features.Accounts;

public static class Create
{
    public class Endpoint : IEndpoint
    {
        public static void Map(IEndpointRouteBuilder app)
        {
            app.MapPost("/create", HandleAsync)
                .WithRequestValidation<Request>()
                .Produces<Response>(StatusCodes.Status201Created)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .ProducesProblem(StatusCodes.Status409Conflict);
        }
    }

    public record Request(string Name, decimal Balance, string Currency);

    public record Response(Guid Id, string Name, decimal Balance, string Currency);

    public class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required.")
                .MinimumLength(3).WithMessage("Name must be at least 3 characters long.")
                .MaximumLength(50).WithMessage("Name must be at most 50 characters long.");

            RuleFor(x => x.Balance)
                .GreaterThanOrEqualTo(0).WithMessage("Balance must be zero or positive.");

            RuleFor(x => x.Currency)
                .NotEmpty().WithMessage("Currency is required.")
                .Must(value => Enum.TryParse<Currency>(value, false, out _))
                .WithMessage("Invalid currency.");
        }
    }

    private static async Task<Results<Created<Response>, ProblemHttpResult>> HandleAsync(
        Request request,
        ApplicationDbContext dbContext,
        ClaimsPrincipal claimsPrincipal,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<Currency>(request.Currency, false, out var currencyEnum))
            return TypedResults.Problem(
                title: "Invalid Currency",
                detail: $"The currency '{request.Currency}' is not supported.",
                statusCode: StatusCodes.Status400BadRequest);

        var userId = claimsPrincipal.GetUserId();

        var nameExist = await dbContext.Accounts
            .AnyAsync(x => x.UserId == userId && x.Name == request.Name, cancellationToken);

        if (nameExist)
            return TypedResults.Problem(
                title: "Account Name Already Exists",
                detail: $"An account with the name '{request.Name}' already exists for this user.",
                statusCode: StatusCodes.Status409Conflict);

        var account = new Account
        {
            UserId = userId,
            Name = request.Name,
            Balance = request.Balance,
            Currency = currencyEnum
        };

        dbContext.Accounts.Add(account);
        await dbContext.SaveChangesAsync(cancellationToken);

        var location =
            $"{httpContext.Request.Scheme}://{httpContext.Request.Host}/{Routes.Prefix}/{Routes.Segments.Accounts}/{account.Id}";

        return TypedResults.Created(location,
            new Response(account.Id, account.Name, account.Balance, account.Currency.ToString()));
    }
}