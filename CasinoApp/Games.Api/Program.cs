using Games.Api.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddDbContext<GamesDbContext>(options => 
    options.UseNpgsql(builder.Configuration.GetConnectionString("casino-games-db")));
builder.EnrichNpgsqlDbContext<GamesDbContext>();

builder.Services.AddHttpClient("WalletApi", client =>
{
    client.BaseAddress = new Uri("https+http://wallet-api");
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<GamesDbContext>();
    dbContext.Database.Migrate();
}

app.UseHttpsRedirection();

app.Run();