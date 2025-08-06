using System.Security.Cryptography;

namespace ExpensoServer.Features.Auth.Constants;

public static class PasswordHasherParameters
{
    public const int SaltSize = 16;
    public const int HashSize = 32;
    public const int Iterations = 100_000;
    public static readonly HashAlgorithmName HashAlgorithmName = HashAlgorithmName.SHA256;
}