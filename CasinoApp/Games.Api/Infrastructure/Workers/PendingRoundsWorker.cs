using Games.Api.Domain;
using Games.Api.Domain.Enums;
using Games.Api.Features.Common;
using Games.Api.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Games.Api.Infrastructure.Workers;

public sealed class PendingRoundsWorker(
    IServiceScopeFactory scopeFactory,
    TimeProvider time,
    ILogger<PendingRoundsWorker> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan MinAge = TimeSpan.FromSeconds(10);
    private const int BatchSize = 50;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval, time);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogError(ex, "Error while retrying pending rounds");
            }
        }
    }

    private async Task ProcessBatchAsync(CancellationToken ct)
    {
        var cutoff = time.GetUtcNow() - MinAge;

        List<Guid> roundIds;
        using (var scope = scopeFactory.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<GamesDbContext>();

            roundIds = await db.Rounds.AsNoTracking()
                .Where(r => r.Status == RoundStatus.Pending && r.CreatedAt < cutoff)
                .OrderBy(r => r.CreatedAt)
                .Select(r => r.Id)
                .Take(BatchSize)
                .ToListAsync(ct);
        }

        foreach (var roundId in roundIds)
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<GamesDbContext>();
            var settler = scope.ServiceProvider.GetRequiredService<RoundSettler>();

            var round = await db.Rounds.SingleOrDefaultAsync(r => r.Id == roundId, ct);
            if (round is null || round.Status != RoundStatus.Pending)
                continue;

            var result = await settler.SettleAsync(round, ct);

            if (result.Status == RoundStatus.Pending)
            {
                logger.LogInformation("Wallet still unavailable; remaining rounds will be retried later");
                break;
            }

            logger.LogInformation("Round {RoundId} completed by worker as {Status}", round.Id, result.Status);
        }
    }
}