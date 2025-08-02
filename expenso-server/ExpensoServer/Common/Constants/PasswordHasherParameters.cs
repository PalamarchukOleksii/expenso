using System.Security.Cryptography;

namespace ExpensoServer.Common.Api.Constants;

public static class PasswordHasherParameters
{
    public const int SaltSize = 16;
    public const int HashSize = 32;
    public const int Iterations = 100_000;
    public static readonly HashAlgorithmName _hashAlgorithmName = HashAlgorithmName.SHA256;
}