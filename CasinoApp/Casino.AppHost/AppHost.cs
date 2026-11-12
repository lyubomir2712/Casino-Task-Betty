var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithHostPort(5432)
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);

var walletDatabase = postgres.AddDatabase("casino-wallet-db");
var gamesDatabase = postgres.AddDatabase("casino-games-db");

var gamesApi = builder.AddProject<Projects.Games_Api>("Games-Api")
    .WithReference(gamesDatabase)
    .WaitFor(gamesDatabase);

var walletApi = builder.AddProject<Projects.Wallet_Api>("Wallet-Api")
    .WithHttpsEndpoint(5001, name: "public")
    .WithReference(walletDatabase)
    .WithReference(gamesApi)
    .WaitFor(walletDatabase)
    .WaitFor(gamesApi);

builder.Build().Run();