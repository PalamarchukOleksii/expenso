using System.Security.Claims;
using ExpensoServer.Common.Api;
using ExpensoServer.Common.Api.Constants;
using ExpensoServer.Common.Api.Extensions;
using ExpensoServer.Common.Api.Filters;
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
                .AddEndpointFilter<RequestValidationFilter<Request>>();
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

    private static async Task<IResult> HandleAsync(
        Request request,
        ApplicationDbContext dbContext,
        ClaimsPrincipal claimsPrincipal,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<Currency>(request.Currency, ignoreCase: false, out var currencyEnum))
            return TypedResults.BadRequest();
        
        var userId = claimsPrincipal.GetUserId();

        var existedAccount =
            await dbContext.Accounts.FirstOrDefaultAsync(x => x.UserId == userId && x.Name == request.Name,
                cancellationToken);
        if (existedAccount is not null)
            return TypedResults.Conflict();

        var account = new Account
        {
            UserId = userId,
            Name = request.Name,
            Balance = request.Balance,
            Currency = currencyEnum
        };

        dbContext.Accounts.Add(account);
        await dbContext.SaveChangesAsync(cancellationToken);

        return TypedResults.Created(
            $"{httpContext.Request.Scheme}://{httpContext.Request.Host}/{ApiRoutes.Prefix}/{ApiRoutes.Segments.Accounts}/{account.Id}",
            new Response(account.Id, account.Name, account.Balance, account.Currency.ToString()));
    }
}