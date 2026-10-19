using BitWrite.OcelotConsole.Application.Interfaces;

namespace BitWrite.OcelotConsole.Application.Interfaces;

public interface IOcelotConfigApplier
{
    Task ApplyAsync(string configuration, CancellationToken cancellationToken = default);
}
