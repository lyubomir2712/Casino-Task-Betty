var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    //.WithHostPort(5432)
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);

var walletDatabase = postgres.AddDatabase("casino-wallet-db");
var gamesDatabase = postgres.AddDatabase("casino-games-db");

var walletApi = builder.AddProject<Projects.Wallet_Api>("Wallet-Api")
    .WithReference(walletDatabase)
    .WaitFor(walletDatabase);

var gamesApi = builder.AddProject<Projects.Games_Api>("Games-Api")
    .WithReference(gamesDatabase)
    .WaitFor(gamesDatabase)
    .WithReference(walletApi)
    .WaitFor(walletApi);

builder.AddProject<Projects.Casino_Gateway>("gateway")
    .WithReference(walletApi).WaitFor(walletApi)
    .WithReference(gamesApi).WaitFor(gamesApi)
    .WithExternalHttpEndpoints();

builder.Build().Run();