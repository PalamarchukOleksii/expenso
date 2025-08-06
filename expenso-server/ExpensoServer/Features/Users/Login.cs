using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ExpensoServer.Common.Api;
using ExpensoServer.Common.Api.Extensions;
using ExpensoServer.Common.Api.Filters;
using ExpensoServer.Data;
using ExpensoServer.Data.Entities;
using ExpensoServer.Features.Users.Constants;
using FluentValidation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace ExpensoServer.Features.Users;

public static class Login
{
    public class Endpoint : IEndpoint
    {
        public static void Map(IEndpointRouteBuilder app)
        {
            app.MapPost("/login", HandleAsync)
                .AddEndpointFilter<RequestValidationFilter<Request>>();
        }
    }

    public record Request(string Email, string Password);

    public record Response(Guid Id, string Email);

    public class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("Invalid email format.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required.");
        }
    }

    private static async Task<IResult> HandleAsync(
        Request request,
        HttpContext httpContext,
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Email == request.Email, cancellationToken);
        if (user is null || !VerifyHashedPassword(user.PasswordHash, user.PasswordSalt, request.Password))
            return TypedResults.Unauthorized();

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

        return TypedResults.Ok(new Response(user.Id, user.Email));
    }

    private static bool VerifyHashedPassword(byte[] storedHash, byte[] storedSalt, string providedPassword)
    {
        var passwordBytes = Encoding.UTF8.GetBytes(providedPassword);

        try
        {
            var actualHash = Rfc2898DeriveBytes.Pbkdf2(
                passwordBytes,
                storedSalt,
                PasswordHasherParameters.Iterations,
                PasswordHasherParameters.HashAlgorithmName,
                storedHash.Length
            );

            return CryptographicOperations.FixedTimeEquals(storedHash, actualHash);
        }
        finally
        {
            Array.Clear(passwordBytes);
        }
    }
}