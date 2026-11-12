using CasinoApp.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddDbContext<WalletDbContext>(options => 
    options.UseNpgsql(builder.Configuration.GetConnectionString("casino-wallet-db")));
builder.EnrichNpgsqlDbContext<WalletDbContext>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<WalletDbContext>();
    dbContext.Database.Migrate();
}

app.UseHttpsRedirection();

app.Run();