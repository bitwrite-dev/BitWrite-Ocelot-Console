using System.Collections.ObjectModel;

namespace BitWrite.OcelotConsole.Application.Permissions;

public sealed class SourceCodePermissionCatalog : IPermissionCatalog
{
    private readonly IReadOnlyDictionary<string, PermissionDefinition> _definitions;
    private readonly IReadOnlyCollection<PermissionDefinition> _all;

    public SourceCodePermissionCatalog(IEnumerable<PermissionDefinition> definitions)
    {
        if (definitions is null)
            throw new ArgumentNullException(nameof(definitions));

        var map = new Dictionary<string, PermissionDefinition>(StringComparer.Ordinal);
        foreach (var definition in definitions)
        {
            if (definition is null)
                throw new ArgumentException("Permission catalog definitions cannot contain null entries.", nameof(definitions));

            var code = PermissionCode.Normalize(definition.Code);
            if (string.IsNullOrWhiteSpace(code))
                throw new ArgumentException("Permission catalog definitions must have a non-empty code.", nameof(definitions));

            if (map.ContainsKey(code))
                throw new ArgumentException($"Duplicate permission code '{code}' in catalog definitions.", nameof(definitions));

            var normalized = definition with
            {
                Code = code,
                Name = (definition.Name ?? string.Empty).Trim(),
                Description = string.IsNullOrWhiteSpace(definition.Description) ? null : definition.Description.Trim(),
                Category = string.IsNullOrWhiteSpace(definition.Category) ? null : definition.Category.Trim()
            };

            map[code] = normalized;
        }

        _definitions = new ReadOnlyDictionary<string, PermissionDefinition>(map);
        _all = Array.AsReadOnly(_definitions.Values.OrderBy(d => d.Code, StringComparer.Ordinal).ToArray());
    }

    public PermissionLookupResult Lookup(string? code)
    {
        var normalized = PermissionCode.Normalize(code);
        if (string.IsNullOrWhiteSpace(normalized))
            return PermissionLookupResult.Unknown(normalized);

        return _definitions.TryGetValue(normalized, out var definition)
            ? PermissionLookupResult.Known(definition)
            : PermissionLookupResult.Unknown(normalized);
    }

    public PermissionDefinition? GetDefinition(string? code)
    {
        var normalized = PermissionCode.Normalize(code);
        if (string.IsNullOrWhiteSpace(normalized))
            return null;

        return _definitions.TryGetValue(normalized, out var definition) ? definition : null;
    }

    public bool IsKnown(string? code) => Lookup(code).IsKnown;
    public bool IsActive(string? code) => Lookup(code).IsActive;
    public bool IsAssignable(string? code) => Lookup(code).IsAssignable;
    public IReadOnlyCollection<PermissionDefinition> GetAll() => _all;
}
