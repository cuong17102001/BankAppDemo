using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using MediatR;
using AccountService.Application.Commands.OpenAccount;
using AccountService.Application.Commands.AddAlias;
using AccountService.Application.Commands.ReserveFunds;
using AccountService.Application.Commands.Debit;
using AccountService.Application.Commands.ReleaseHold;
using AccountService.Application.Queries.GetBalance;
using AccountService.Domain.Exceptions;
using AccountService.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Auth: JWT bearer
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.Authority = "https://localhost:7001";
    options.RequireHttpsMetadata = true;
    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = "https://localhost:7001",
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("account.read", policy => policy.RequireClaim("scope", "account.read"));
    options.AddPolicy("account.write", policy => policy.RequireClaim("scope", "account.write"));
    // Role policies
    options.AddPolicy("role.admin", p => p.RequireRole("admin"));
    options.AddPolicy("role.staff", p => p.RequireRole("staff"));
    options.AddPolicy("role.customer", p => p.RequireRole("customer"));
});

// EF Core InMemory for demo
builder.Services.AddDbContext<AccountDbContext>(opt =>
    opt.UseInMemoryDatabase("accounts"));

// DI
builder.Services.AddScoped<IAccountRepository, AccountRepository>();
builder.Services.AddScoped<AccountService.Infrastructure.ReadModels.IAccountReadRepository, AccountService.Infrastructure.ReadModels.InMemoryAccountReadRepository>();
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(OpenAccountCommand).Assembly));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

// Sample role-protected APIs (outside identity)
app.MapGet("/rbac/a", () => Results.Ok(new { message = "API A - admin only (AccountService)" }))
    .RequireAuthorization("role.admin");

app.MapGet("/rbac/b", () => Results.Ok(new { message = "API B - admin and staff (AccountService)" }))
    .RequireAuthorization(policy => policy.RequireRole("admin", "staff"));

app.MapGet("/rbac/c", () => Results.Ok(new { message = "API C - all roles (AccountService)" }))
    .RequireAuthorization(policy => policy.RequireRole("admin", "staff", "customer"));

// Endpoints
app.MapPost("/accounts", async ([FromBody] OpenAccountRequest req, IMediator mediator) =>
{
    var account = await mediator.Send(new OpenAccountCommand(req.CustomerId, req.Currency));
    return Results.Created($"/accounts/{account.Id}", new { account.Id, account.Currency, account.Status });
}).RequireAuthorization("account.write");

app.MapPost("/accounts/{id:guid}/aliases", async (Guid id, [FromBody] AddAliasRequest req, IMediator mediator) =>
{
    var alias = await mediator.Send(new AddAliasCommand(id, req.Type, req.Value));
    return Results.Created($"/accounts/{id}/aliases/{alias.Id}", alias);
}).RequireAuthorization("account.write");

app.MapPost("/accounts/{id:guid}/reserve", async (Guid id, [FromBody] ReserveRequest req, IMediator mediator) =>
{
    try
    {
        var hold = await mediator.Send(new ReserveFundsCommand(id, req.Amount, req.Reference));
        return Results.Created($"/accounts/{id}/holds/{hold.Id}", hold);
    }
    catch (DomainException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
}).RequireAuthorization("account.write");

app.MapPost("/accounts/{id:guid}/debit", async (Guid id, [FromBody] DebitRequest req, IMediator mediator) =>
{
    try
    {
        await mediator.Send(new DebitCommand(id, req.Amount));
        var b = await mediator.Send(new GetBalanceQuery(id));
        return Results.Ok(new { balance = b.balance, available = b.available });
    }
    catch (DomainException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
}).RequireAuthorization("account.write");

app.MapPost("/accounts/{id:guid}/release", async (Guid id, [FromBody] ReleaseRequest req, IMediator mediator) =>
{
    try
    {
        await mediator.Send(new ReleaseHoldCommand(id, req.HoldId));
        return Results.Ok();
    }
    catch (DomainException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
}).RequireAuthorization("account.write");

app.MapGet("/accounts/{id:guid}/balance", async (Guid id, IMediator mediator) =>
{
    var b = await mediator.Send(new GetBalanceQuery(id));
    return Results.Ok(new { balance = b.balance, available = b.available, currency = b.currency });
}).RequireAuthorization("account.read");

app.Run();

// DTOs
record OpenAccountRequest(Guid CustomerId, string Currency);
record AddAliasRequest(string Type, string Value);
record ReserveRequest(decimal Amount, string Reference);
record DebitRequest(decimal Amount);
record ReleaseRequest(Guid HoldId);
