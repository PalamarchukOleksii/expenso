using System.Security.Cryptography;
using System.Text;
using ExpensoServer.Common.Api;
using ExpensoServer.Common.Api.Constants;
using ExpensoServer.Common.Api.Extensions;
using ExpensoServer.Common.Api.Filters;
using ExpensoServer.Data;
using ExpensoServer.Data.Entities;
using ExpensoServer.Features.Users.Constants;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace ExpensoServer.Features.Users;

public static class Register
{
    public class Endpoint : IEndpoint
    {
        public static void Map(IEndpointRouteBuilder app)
        {
            app.MapPost("/register", HandleAsync)
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
                .NotEmpty().WithMessage("Password is required.")
                .MinimumLength(8).WithMessage("Password must be at least 8 characters long.")
                .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
                .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
                .Matches("[0-9]").WithMessage("Password must contain at least one digit.")
                .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character.");
        }
    }

    private static async Task<IResult> HandleAsync(
        Request request,
        ApplicationDbContext dbContext,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (await dbContext.Users.AnyAsync(x => x.Email == request.Email, cancellationToken))
            return TypedResults.Conflict();

        var hashingResult = HashPassword(request.Password);

        var user = new User
        {
            Email = request.Email,
            PasswordHash = hashingResult.Hash,
            PasswordSalt = hashingResult.Salt,
        };

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);

        return TypedResults.Created(
            $"{httpContext.Request.Scheme}://{httpContext.Request.Host}/{ApiRoutes.Prefix}/{ApiRoutes.Segments.Users}/{user.Id}",
            new Response(user.Id, user.Email));
    }

    private static  PasswordHashResult HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(PasswordHasherParameters.SaltSize);
        var passwordBytes = Encoding.UTF8.GetBytes(password);

        try
        {
            var hash = Rfc2898DeriveBytes.Pbkdf2(
                passwordBytes,
                salt,
                PasswordHasherParameters.Iterations,
                PasswordHasherParameters.HashAlgorithmName,
                PasswordHasherParameters.HashSize
            );

            return new PasswordHashResult(salt, hash);
        }
        finally
        {
            Array.Clear(passwordBytes); 
        }
    }
    
    private record PasswordHashResult(byte[] Salt, byte[] Hash);
}