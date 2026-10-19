using BitWrite.OcelotConsole.Application.Interfaces;
using BitWrite.OcelotConsole.Domain.Services;
using BitWrite.OcelotConsole.Domain.ValueObjects.Configuration;
using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;

namespace BitWrite.OcelotConsole.Infrastructure.Adapters;

/// <summary>
/// Adapter that implements ISnapshotIntegrityVerifier using Domain.SnapshotIntegrityVerifier.
/// </summary>
public class SnapshotIntegrityVerifierAdapter : ISnapshotIntegrityVerifier
{
    private readonly SnapshotIntegrityVerifier _domainVerifier;

    public SnapshotIntegrityVerifierAdapter(SnapshotIntegrityVerifier domainVerifier)
    {
        _domainVerifier = domainVerifier;
    }

    public bool VerifyHash(string snapshotContent, ConfigurationHash expectedHash)
    {
        return _domainVerifier.VerifyHash(snapshotContent, expectedHash);
    }

    public ConfigurationHash ComputeHash(string snapshotContent)
    {
        return _domainVerifier.ComputeHash(snapshotContent);
    }

    public bool VerifyImmutability(string snapshotContent, ConfigurationHash originalHash)
    {
        return _domainVerifier.VerifyImmutability(snapshotContent, originalHash);
    }

    public void ValidateIntegrity(string snapshotContent, ConfigurationHash expectedHash, SnapshotVersion version)
    {
        _domainVerifier.ValidateIntegrity(snapshotContent, expectedHash, version);
    }
}
