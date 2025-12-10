using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using MediatR;
using TransactionService.Infrastructure;
using TransactionService.Domain.Entities;
using TransactionService.Infrastructure.Events;
using TransactionService.Domain.Events;
using System.Text.Json;

// In-memory mock hold store
var holds = new Dictionary<Guid, decimal>();

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
    options.AddPolicy("transaction.read", policy => policy.RequireClaim("scope", "transaction.read"));
    options.AddPolicy("transaction.write", policy => policy.RequireClaim("scope", "transaction.write"));
});

builder.Services.AddDbContext<LedgerDbContext>(opt => opt.UseInMemoryDatabase("ledger"));

builder.Services.AddScoped<ILedgerRepository, LedgerRepository>();
builder.Services.AddScoped<IEventBus, InMemoryEventBus>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

// Create transaction
app.MapPost("/transactions", async ([FromBody] CreateTransactionRequest req, ILedgerRepository repo) =>
{
    var tx = new Transaction { Type = req.Type, InitiatedBy = req.InitiatedBy };
    int seq = 1;
    foreach (var e in req.Entries)
    {
        tx.Entries.Add(new LedgerEntry
        {
            TransactionId = tx.Id,
            AccountId = e.AccountId,
            EntryType = e.EntryType,
            Amount = e.Amount,
            Currency = e.Currency,
            Sequence = seq++
        });
    }
    await repo.AddAsync(tx);
    return Results.Created($"/transactions/{tx.Id}", new { tx.Id, tx.Status, tx.Type });
}).RequireAuthorization("transaction.write");

// Outbox dispatcher (background)
app.Lifetime.ApplicationStarted.Register(() =>
{
    var scopeFactory = app.Services.GetRequiredService<IServiceScopeFactory>();
    Task.Run(async () =>
    {
        while (true)
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<LedgerDbContext>();
            var bus = scope.ServiceProvider.GetRequiredService<IEventBus>();
            var pending = await db.OutboxMessages.Where(x => x.PublishedAt == null).ToListAsync();
            foreach (var m in pending)
            {
                try
                {
                    if (m.Type == typeof(TransactionPosted).FullName)
                    {
                        var evt = JsonSerializer.Deserialize<TransactionPosted>(m.Payload)!;
                        await bus.PublishAsync(evt);
                    }
                    m.PublishedAt = DateTimeOffset.UtcNow;
                    await db.SaveChangesAsync();
                }
                catch
                {
                    // keep for retry
                }
            }
            await Task.Delay(1000);
        }
    });
});

// Commit transaction (immutable ledger write)
app.MapPost("/transactions/{id:guid}/commit", async (Guid id, ILedgerRepository repo) =>
{
    var tx = await repo.GetAsync(id);
    if (tx == null) return Results.NotFound();
    try
    {
        tx.Commit();
        await repo.CommitAsync(tx);
        return Results.Ok(new { tx.Id, tx.Status });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
}).RequireAuthorization("transaction.write");

// Reverse transaction
app.MapPost("/transactions/{id:guid}/reverse", async (Guid id, ILedgerRepository repo) =>
{
    var tx = await repo.GetAsync(id);
    if (tx == null) return Results.NotFound();
    try
    {
        tx.Reverse();
        await repo.CommitAsync(tx);
        return Results.Ok(new { tx.Id, tx.Status });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
}).RequireAuthorization("transaction.write");

// Transfer Saga (mocked)
app.MapPost("/transfer", async ([FromBody] TransferRequest req, ILedgerRepository ledger, IEventBus bus) =>
{
    // AML mock: reject if amount exceeds threshold
    const decimal amlThreshold = 100000m;
    if (req.Amount <= 0) return Results.BadRequest(new { error = "Amount must be positive" });
    if (req.Amount > amlThreshold)
        return Results.BadRequest(new { error = "AML blocked" });

    // Mock reserve funds on source
    if (holds.TryGetValue(req.SourceAccountId, out var reserved))
    {
        holds[req.SourceAccountId] = reserved + req.Amount;
    }
    else
    {
        holds[req.SourceAccountId] = req.Amount;
    }

    // Create transaction
    var tx = new Transaction { Type = "transfer", InitiatedBy = req.InitiatedBy };
    tx.Entries.Add(new LedgerEntry
    {
        TransactionId = tx.Id,
        AccountId = req.SourceAccountId,
        EntryType = EntryType.Debit,
        Amount = req.Amount,
        Currency = req.Currency,
        Sequence = 1
    });
    tx.Entries.Add(new LedgerEntry
    {
        TransactionId = tx.Id,
        AccountId = req.TargetAccountId,
        EntryType = EntryType.Credit,
        Amount = req.Amount,
        Currency = req.Currency,
        Sequence = 2
    });

    try
    {
        await ledger.AddAsync(tx);
        tx.Commit();
        await ledger.CommitAsync(tx);
        // publish TransactionPosted already handled inside repository
        // release mock hold after successful commit
        holds[req.SourceAccountId] = Math.Max(0, holds[req.SourceAccountId] - req.Amount);
        return Results.Ok(new { tx.Id, tx.Status });
    }
    catch (Exception ex)
    {
        // rollback: release reserved funds
        if (holds.TryGetValue(req.SourceAccountId, out var res))
            holds[req.SourceAccountId] = Math.Max(0, res - req.Amount);
        return Results.BadRequest(new { error = ex.Message });
    }
}).RequireAuthorization("transaction.write");

app.Run();

// DTOs
record CreateTransactionRequest(string Type, Guid? InitiatedBy, List<CreateTransactionEntry> Entries);
record CreateTransactionEntry(Guid AccountId, EntryType EntryType, decimal Amount, string Currency);

// DTOs for transfer
record TransferRequest(Guid SourceAccountId, Guid TargetAccountId, decimal Amount, string Currency, Guid? InitiatedBy);
