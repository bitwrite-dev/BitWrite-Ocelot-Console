using BitWrite.OcelotConsole.Application.UseCases.Identity.Bootstrap;
using Microsoft.Extensions.Options;

namespace BitWrite.OcelotConsole.Api.HostedServices;

public class BootstrapInitialAdminHostedService : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<BootstrapInitialAdminOptions> _options;
    private readonly ILogger<BootstrapInitialAdminHostedService> _logger;

    public BootstrapInitialAdminHostedService(
        IServiceScopeFactory scopeFactory,
        IOptions<BootstrapInitialAdminOptions> options,
        ILogger<BootstrapInitialAdminHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_options.Value.Enabled)
            return;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<BootstrapInitialAdminCommandHandler>();
            var result = await handler.HandleAsync(new BootstrapInitialAdminCommand("bootstrap:startup", Guid.NewGuid().ToString()), cancellationToken);
            _logger.LogInformation("IAM bootstrap startup result: completed={Completed} created={Created} message={Message}", result.Completed, result.Created, result.Message);
        }
        catch (Exception ex)
        {
            // Startup should not crash the whole API for bootstrap misconfiguration in environments where auth is managed externally;
            // but security-sensitive deployments can monitor logs/audit. Do not log secrets.
            _logger.LogError(ex, "IAM bootstrap startup failed.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
