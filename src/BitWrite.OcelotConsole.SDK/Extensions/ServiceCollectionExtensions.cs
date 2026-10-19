using System;
using System.Net.Http;
using BitWrite.OcelotConsole.SDK.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BitWrite.OcelotConsole.SDK.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOcelotConsoleSdk(this IServiceCollection services, Action<OcelotConsoleClientOptions> configureOptions)
    {
        if (configureOptions == null)
            throw new ArgumentNullException(nameof(configureOptions));

        services.Configure(configureOptions);
        services.AddHttpClient<OcelotConsoleClient>((sp, client) =>
        {
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<OcelotConsoleClientOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseAddress);
            client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        });

        services.AddSingleton<IJwtTokenProvider, JwtTokenProvider>();
        services.AddTransient<OcelotConsoleClient>();

        return services;
    }

    public static IServiceCollection AddOcelotConsoleSdk(this IServiceCollection services, string baseAddress, string environment)
    {
        return services.AddOcelotConsoleSdk(options =>
        {
            options.BaseAddress = baseAddress;
            options.Environment = environment;
        });
    }
}
