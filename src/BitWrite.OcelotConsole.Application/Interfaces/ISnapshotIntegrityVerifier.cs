using BitWrite.OcelotConsole.Domain.Services;
using BitWrite.OcelotConsole.Domain.ValueObjects.Configuration;
using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;

namespace BitWrite.OcelotConsole.Application.Interfaces;

public interface ISnapshotIntegrityVerifier
{
    bool VerifyHash(string snapshotContent, ConfigurationHash expectedHash);
    ConfigurationHash ComputeHash(string snapshotContent);
    bool VerifyImmutability(string snapshotContent, ConfigurationHash originalHash);
    void ValidateIntegrity(string snapshotContent, ConfigurationHash expectedHash, SnapshotVersion version);
}
