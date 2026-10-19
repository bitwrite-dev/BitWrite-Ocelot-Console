namespace BitWrite.OcelotConsole.Application.Interfaces;

public interface IRedisPublisher
{
    Task PublishAsync(string channel, string message, CancellationToken cancellationToken = default);
    Task PublishAsync<T>(string channel, T message, CancellationToken cancellationToken = default) where T : class;
}
