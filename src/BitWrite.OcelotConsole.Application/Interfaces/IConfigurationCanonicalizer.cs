using BitWrite.OcelotConsole.Domain.Services;

namespace BitWrite.OcelotConsole.Application.Interfaces;

public interface IConfigurationCanonicalizer
{
    string Canonicalize(OcelotConfiguration configuration);
    string CanonicalizeJson(OcelotConfiguration configuration);
}
