using System;
using ExpensoServer.Features.Users;
using ExpensoServer.Shared;
using ExpensoServer.Shared.Enums;

namespace ExpensoServer.Models;

public class Account
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; } = Guid.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Balance { get; set; } = 0.0m;
    public Currency Currency { get; set; } = Currency.UAH;

    public User User { get; set; } = new();
}
