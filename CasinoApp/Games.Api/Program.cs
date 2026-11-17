using Casino.ServiceDefault;
using Games.Api.Domain;
using Games.Api.Domain.Interfaces;
using Games.Api.Domain.Rules;
using Games.Api.Features.Common;
using Games.Api.Features.GetRound;
using Games.Api.Features.PlaceBet;
using Games.Api.Infrastructure.Random;
using Games.Api.Infrastructure.Wallet;
using Games.Api.Infrastructure.Workers;
using Games.Api.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddOpenApi();

builder.Services.AddDbContext<GamesDbContext>(options => 
    options.UseNpgsql(builder.Configuration.GetConnectionString("casino-games-db")));
builder.EnrichNpgsqlDbContext<GamesDbContext>();

builder.Services.AddHttpClient("WalletApi", client =>
{
    client.BaseAddress = new Uri("https+http://Wallet-Api");
});

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IRandomSource, CryptoRandomSource>();
builder.Services.AddSingleton<OutcomeCalculator>();
builder.Services.AddScoped<RoundSettler>();
builder.Services.AddHostedService<PendingRoundsWorker>();
builder.Services.AddHttpClient<WalletClient>(client =>
    client.BaseAddress = new Uri("https+http://Wallet-Api"));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    
    app.MapOpenApi();
    
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<GamesDbContext>();
    dbContext.Database.Migrate();
}

PlaceBet.Map(app);
GetRound.Map(app);

app.UseHttpsRedirection();

app.Run();