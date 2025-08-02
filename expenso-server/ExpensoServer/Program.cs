using ExpensoServer;
using ExpensoServer.Data;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServices();

var app = builder.Build();

app.Configure();

app.Run();
