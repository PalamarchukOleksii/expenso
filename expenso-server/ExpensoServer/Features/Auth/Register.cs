using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using ExpensoServer.Common.Abstractions;
using ExpensoServer.Common.Constants;
using ExpensoServer.Common.Filters;
using ExpensoServer.Data;
using ExpensoServer.Data.Entities;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace ExpensoServer.Features.Auth;

public static class Register
{
    public class Endpoint : IEndpoint
    {
        public static void Map(IEndpointRouteBuilder app)
        {
            app.MapPost("/register", HandleAsync)
                .AddEndpointFilter<RequestValidationFilter<Request>>()
                .Produces<Response>(StatusCodes.Status201Created)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .ProducesProblem(StatusCodes.Status409Conflict);
        }
    }

    public class Request
    {
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        [Required(ErrorMessage = "Email is required.")]
        public required string Email { get; set; }

        [StringLength(256, MinimumLength = 8, ErrorMessage = "Password must be between 8 and 256 characters long.")]
        [Required(ErrorMessage = "Password is required.")]
        [PasswordValidation]
        public required string Password { get; set; }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public sealed class PasswordValidationAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is not string password)
                return ValidationResult.Success;

            if (!password.Any(char.IsUpper))
                return new ValidationResult("Password must contain at least one uppercase letter.");

            if (!password.Any(char.IsLower))
                return new ValidationResult("Password must contain at least one lowercase letter.");

            if (!password.Any(char.IsDigit))
                return new ValidationResult("Password must contain at least one digit.");

            if (!password.Any(c => !char.IsLetterOrDigit(c)))
                return new ValidationResult("Password must contain at least one special character.");

            return ValidationResult.Success;
        }
    }

    public record Response(Guid Id, string Email);

    private static async Task<Results<Created<Response>, ProblemHttpResult>> HandleAsync(
        Request request,
        ApplicationDbContext dbContext,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (await dbContext.Users.AnyAsync(x => x.Email == request.Email, cancellationToken))
            return TypedResults.Problem(
                title: "Email Conflict",
                detail: $"User with email '{request.Email}' already exists.",
                statusCode: StatusCodes.Status409Conflict);

        var hashingResult = HashPassword(request.Password);

        var user = new User
        {
            Email = request.Email,
            PasswordHash = hashingResult.Hash,
            PasswordSalt = hashingResult.Salt
        };

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);

        var location =
            $"{httpContext.Request.Scheme}://{httpContext.Request.Host}/{EndpointRoutes.Prefix}/{EndpointRoutes.Segments.Users}/{user.Id}";

        return TypedResults.Created(location, new Response(user.Id, user.Email));
    }

    private static PasswordHashResult HashPassword(string password)
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