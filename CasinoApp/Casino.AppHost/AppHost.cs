var builder = DistributedApplication.CreateBuilder(args);
builder.AddProject<Projects.Games_Api>("Games-Api");
builder.AddProject<Projects.Wallet_Api>("Wallet-Api");
    builder.Build().Run();