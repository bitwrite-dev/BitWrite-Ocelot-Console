using BitWrite.OcelotConsole.Application.Interfaces;
using BitWrite.OcelotConsole.Application.UseCases.Publication;
using BitWrite.OcelotConsole.Domain.Aggregates.Publication;
using BitWrite.OcelotConsole.Domain.Aggregates.Snapshot;
using BitWrite.OcelotConsole.Domain.ValueObjects.Configuration;
using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;
using BitWrite.OcelotConsole.Domain.ValueObjects.Status;

namespace BitWrite.OcelotConsole.Application.UseCases.Publication;

public class GetPublicationDiffQueryHandler
{
    private readonly ISnapshotRepository _snapshotRepository;
    private readonly IPublicationRepository _publicationRepository;

    public GetPublicationDiffQueryHandler(
        ISnapshotRepository snapshotRepository,
        IPublicationRepository publicationRepository)
    {
        _snapshotRepository = snapshotRepository;
        _publicationRepository = publicationRepository;
    }

    public async Task<PublicationDiffResponse> HandleAsync(GetPublicationDiffQuery query, CancellationToken cancellationToken = default)
    {
        // Get target snapshot
        if (!int.TryParse(query.TargetSnapshotVersion, out var targetVersionInt))
        {
            throw new ArgumentException($"Invalid target snapshot version: {query.TargetSnapshotVersion}");
        }
        var targetVersion = SnapshotVersion.From(targetVersionInt);
        var targetSnapshot = await _snapshotRepository.GetAsync(targetVersion, cancellationToken);
        if (targetSnapshot == null)
        {
            throw new InvalidOperationException($"Target snapshot {targetVersion} not found");
        }

        // Determine current published snapshot version
        int? currentSnapshotVersion = null;
        if (!string.IsNullOrEmpty(query.CurrentSnapshotVersion) && int.TryParse(query.CurrentSnapshotVersion, out var currentVersionInt))
        {
            currentSnapshotVersion = currentVersionInt;
        }
        else
        {
            // Try to get from the latest publication
            try
            {
                var latestPublication = await _publicationRepository.GetLatestAsync(cancellationToken);
                if (latestPublication != null && latestPublication.Status == PublicationStatus.Published)
                {
                    currentSnapshotVersion = latestPublication.SnapshotVersion.Value;
                }
            }
            catch
            {
                // Ignore - will proceed without current version
            }
        }

        // Generate differences
        var differences = new List<string>();
        var hasDifferences = false;

        // If we have both snapshots, compute diff
        if (currentSnapshotVersion.HasValue)
        {
            var currentSnapshot = await _snapshotRepository.GetAsync(SnapshotVersion.From(currentSnapshotVersion.Value), CancellationToken.None);
            if (currentSnapshot != null)
            {
                // Try to parse JSON content for meaningful diff
                try
                {
                    using var currentDoc = System.Text.Json.JsonDocument.Parse(currentSnapshot.Content);
                    using var targetDoc = System.Text.Json.JsonDocument.Parse(targetSnapshot.Content);

                    var currentRoutes = currentDoc.RootElement.TryGetProperty("Routes", out var cr) ? cr.GetArrayLength() : 0;
                    var targetRoutes = targetDoc.RootElement.TryGetProperty("Routes", out var tr) ? tr.GetArrayLength() : 0;

                    if (currentRoutes != targetRoutes)
                    {
                        hasDifferences = true;
                        differences.Add($"Route count changed: {currentRoutes} → {targetRoutes}");
                    }

                    var currentServices = currentDoc.RootElement.TryGetProperty("Services", out var cs) ? cs.GetArrayLength() : 0;
                    var targetServices = targetDoc.RootElement.TryGetProperty("Services", out var ts) ? ts.GetArrayLength() : 0;

                    if (currentServices != targetServices)
                    {
                        hasDifferences = true;
                        differences.Add($"Service count changed: {currentServices} → {targetServices}");
                    }

                    // Compare global configuration
                    if (currentDoc.RootElement.TryGetProperty("GlobalConfiguration", out var cg) &&
                        targetDoc.RootElement.TryGetProperty("GlobalConfiguration", out var tg))
                    {
                        if (cg.ToString() != tg.ToString())
                        {
                            hasDifferences = true;
                            differences.Add("Global configuration changed");
                        }
                    }

                    if (!hasDifferences)
                    {
                        differences.Add("No configuration changes detected");
                    }
                }
                catch
                {
                    // Fallback to raw content comparison
                    if (currentSnapshot.Content != targetSnapshot.Content)
                    {
                        hasDifferences = true;
                        differences.Add("Configuration content differs from current published version");
                    }
                    else
                    {
                        differences.Add("No configuration changes detected");
                    }
                }
            }
            else
            {
                differences.Add($"Current snapshot {currentSnapshotVersion} not found");
            }
        }
        else
        {
            differences.Add("No current published snapshot to compare against");
            hasDifferences = true;
        }

        return new PublicationDiffResponse(
            HasDifferences: hasDifferences,
            Differences: differences,
            TargetSnapshotVersion: int.Parse(query.TargetSnapshotVersion),
            CurrentSnapshotVersion: currentSnapshotVersion
        );
    }
}
