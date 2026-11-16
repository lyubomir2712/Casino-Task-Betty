using CasinoApp.Features;
using CasinoApp.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddDbContext<WalletDbContext>(options => 
    options.UseNpgsql(builder.Configuration.GetConnectionString("casino-wallet-db")));
builder.EnrichNpgsqlDbContext<WalletDbContext>();

builder.Services.AddSingleton(TimeProvider.System);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<WalletDbContext>();
    dbContext.Database.Migrate();
}

GetOrCreatePlayer.Map(app);
Deposit.Map(app);
Withdraw.Map(app);
GetBalance.Map(app);
SettleRound.Map(app);

app.UseHttpsRedirection();

app.Run();