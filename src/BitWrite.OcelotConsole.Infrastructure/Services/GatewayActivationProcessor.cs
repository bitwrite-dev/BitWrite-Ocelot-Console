using System.Text.Json;
using BitWrite.OcelotConsole.Application.Interfaces;
using BitWrite.OcelotConsole.Domain.Aggregates.Publication;
using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;
using BitWrite.OcelotConsole.Domain.ValueObjects.Status;
using BitWrite.OcelotConsole.Infrastructure.Redis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace BitWrite.OcelotConsole.Infrastructure.Services;

public class GatewayActivationProcessor : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly ILogger<GatewayActivationProcessor> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(2);

    public GatewayActivationProcessor(
        IServiceScopeFactory scopeFactory,
        IConnectionMultiplexer connectionMultiplexer,
        ILogger<GatewayActivationProcessor> logger)
    {
        _scopeFactory = scopeFactory;
        _connectionMultiplexer = connectionMultiplexer;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Gateway Activation Processor started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessActivationResultsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing gateway activation results.");
            }

            await Task.Delay(_interval, stoppingToken);
        }

        _logger.LogInformation("Gateway Activation Processor stopped.");
    }

    private async Task ProcessActivationResultsAsync(CancellationToken cancellationToken)
    {
        var db = _connectionMultiplexer.GetDatabase();
        var key = RedisKeyHelper.GatewayActivationResults;

        var results = await db.ListRangeAsync(key, 0, 99);
        if (results.Length == 0)
            return;

        await db.ListTrimAsync(key, results.Length, -1);

        using var scope = _scopeFactory.CreateScope();
        var publicationRepository = scope.ServiceProvider.GetRequiredService<IPublicationRepository>();
        var gatewayRepository = scope.ServiceProvider.GetRequiredService<IGatewayRepository>();
        var eventDispatcher = scope.ServiceProvider.GetRequiredService<IDomainEventDispatcher>();

        var gateways = await gatewayRepository.GetAllAsync(cancellationToken);
        var gatewayMap = gateways.ToDictionary(g => g.Id, g => g.Name);

        foreach (var result in results)
        {
            try
            {
                if (result.IsNullOrEmpty) continue;
                using var doc = JsonDocument.Parse(result.ToString());
                var root = doc.RootElement;

                if (!root.TryGetProperty("GatewayId", out var gatewayIdEl) ||
                    !root.TryGetProperty("Version", out var versionEl))
                    continue;

                var gatewayIdStr = gatewayIdEl.GetString()!;
                GatewayId gatewayId;
                try
                {
                    gatewayId = GatewayId.From(gatewayIdStr);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Invalid GatewayId format in activation result: {GatewayId}, skipping", gatewayIdStr);
                    continue;
                }
                var versionStr = versionEl.ValueKind == JsonValueKind.String ? versionEl.GetString()! : versionEl.GetInt32().ToString();
                var version = int.Parse(versionStr);
                var success = root.TryGetProperty("Success", out var successEl) ? successEl.GetBoolean() : true;

                var publications = await publicationRepository.GetAllAsync(cancellationToken);
                foreach (var publication in publications)
                {
                    if (publication.SnapshotVersion.Value != version)
                        continue;

                    if (success)
                    {
                        try
                        {
                            publication.RecordGatewayHealthy(gatewayId);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Error recording healthy gateway {GatewayId} for publication {PublicationId}", gatewayId, publication.Id);
                        }
                    }
                    else
                    {
                        var error = root.TryGetProperty("Error", out var errorEl) ? errorEl.GetString() : "Unknown error";
                        try
                        {
                            publication.RecordGatewayFailed(gatewayId, error ?? "Unknown error");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Error recording failed gateway {GatewayId} for publication {PublicationId}", gatewayId, publication.Id);
                        }
                    }

                    await publicationRepository.UpdateAsync(publication, cancellationToken);

                    // Update Redis progress cache
                    await UpdatePublicationProgressCacheAsync(db, publication, gatewayMap, cancellationToken);

                    // Dispatch domain events
                    foreach (var domainEvent in publication.DomainEvents)
                    {
                        await eventDispatcher.DispatchAsync(domainEvent, cancellationToken);
                    }
                    publication.ClearDomainEvents();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to process activation result");
            }
        }
    }

    private async Task UpdatePublicationProgressCacheAsync(
        IDatabase db,
        Publication publication,
        Dictionary<GatewayId, string> gatewayMap,
        CancellationToken cancellationToken)
    {
        var progressKey = $"publication:progress:{publication.Id.Value}";
        var gatewayProgress = publication.GatewayStates.Select(kvp => new
        {
            GatewayId = kvp.Key.Value.ToString(),
            GatewayName = gatewayMap.TryGetValue(kvp.Key, out var name) ? name : kvp.Key.Value.ToString(),
            Status = kvp.Value.Status,
            ReceivedAt = (DateTimeOffset?)kvp.Value.ReceivedAt,
            ValidatedAt = (DateTimeOffset?)kvp.Value.ValidatedAt,
            AppliedAt = (DateTimeOffset?)kvp.Value.AppliedAt,
            HealthyAt = (DateTimeOffset?)kvp.Value.HealthyAt,
            FailedAt = (DateTimeOffset?)kvp.Value.FailedAt,
            IsValid = kvp.Value.IsValid,
            FailureReason = kvp.Value.FailureReason
        }).ToList();

        var progressData = new
        {
            PublicationId = publication.Id.Value.ToString(),
            SnapshotVersion = publication.SnapshotVersion.Value,
            Status = publication.Status.Value,
            ProgressPercentage = publication.ProgressPercentage,
            GatewayProgress = gatewayProgress,
            StartedAt = publication.StartedAt,
            CompletedAt = publication.CompletedAt,
            FailureReason = publication.FailureReason
        };

        await db.StringSetAsync(progressKey, System.Text.Json.JsonSerializer.Serialize(progressData), TimeSpan.FromHours(24));
    }
}
