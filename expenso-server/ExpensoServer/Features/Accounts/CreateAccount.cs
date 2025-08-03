using System.Security.Claims;
using ExpensoServer.Common.Api;
using ExpensoServer.Common.Api.Constants;
using ExpensoServer.Common.Api.Extensions;
using ExpensoServer.Data;
using ExpensoServer.Data.Entities;
using ExpensoServer.Data.Enums;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExpensoServer.Features.Accounts;

public static class CreateAccount
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

    public record Request(string Name, decimal Balance, Currency Currency);

    public record Response(Guid Id, string Name, decimal Balance, Currency Currency);

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
                .IsInEnum().WithMessage("Invalid currency.");
        }
    }

    private static async Task<Results<Created<Response>, ProblemHttpResult>> HandleAsync(
        Request request,
        ApplicationDbContext dbContext,
        ClaimsPrincipal claimsPrincipal,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var userId = claimsPrincipal.GetUserId();

        var existedAccount = await dbContext.GetAccountByUserIdAndNameAsync(userId, request.Name, cancellationToken);
        if (existedAccount is not null)
            return TypedResults.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Conflict",
                detail: $"An account with the name '{request.Name}' already exists for this user.",
                type: "https://tools.ietf.org/html/rfc7231#section-6.5.8"
            );
        
        var account = new Account
        {
            UserId = userId,
            Name = request.Name,
            Balance = request.Balance,
            Currency = request.Currency
        };

        dbContext.Accounts.Add(account);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = new Response(account.Id, account.Name, account.Balance, account.Currency);
        string location = httpContext.GetCreatedAccountLocation(account.Id);
        return TypedResults.Created(location, response);
    }

    private static async Task<Account?> GetAccountByUserIdAndNameAsync(
        this ApplicationDbContext dbContext,
        Guid userId,
        string accountName,
        CancellationToken cancellationToken)
    {
        return await dbContext.Accounts
            .FirstOrDefaultAsync(x => x.UserId == userId && x.Name == accountName, cancellationToken);
    }
    
    private static string GetCreatedAccountLocation(this HttpContext httpContext, Guid accountId)
    {
        return $"{httpContext.Request.Scheme}://{httpContext.Request.Host}/{ApiRoutes.Prefix}/{ApiRoutes.Segments.Accounts}/{accountId}";
    }
}