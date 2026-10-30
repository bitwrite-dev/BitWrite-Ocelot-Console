namespace ocelot.gateway.node3.Configuration;

/// <summary>
/// The environment this node serves, and the names that follow from it.
/// </summary>
/// <remarks>
/// One setting, not two, because the file and the key have to agree: Ocelot reads its
/// configuration from a file named after the environment, and the control plane stores
/// a published snapshot under the environment it was published for. A gateway that
/// wrote <c>ocelot.Staging.json</c> while Ocelot read <c>ocelot.Production.json</c>, or
/// that loaded a production snapshot into a development file, would serve one
/// environment's routes while claiming another's name — and neither would report it.
///
/// Two spellings rather than one because the two sides disagree: Redis keys carry the
/// normalised lower-case name the control plane's value object produces, and Ocelot's
/// file name is spelled the way Ocelot spells it. Both come from the one configured
/// name, so neither is written out by hand at a call site.
///
/// Configurable rather than constant (#529) because a fleet is more than one node:
/// node-2 serving staging is a different environment in the same layout, not a fork of
/// this file. Derived from configuration rather than from
/// <c>ASPNETCORE_ENVIRONMENT</c>: that names how the process is deployed, not which
/// environment's configuration it serves, and the two have to be the same name for the
/// pair to mean anything.
/// </remarks>
public sealed class PublishedEnvironment
{
    /// <summary>Configuration section the node's own settings live under.</summary>
    public const string SectionName = "Gateway";

    /// <summary>Environment variable name, for a container started with one liner.</summary>
    public const string EnvironmentVariable = "GATEWAY_ENVIRONMENT";

    /// <summary>The setting that names the environment this node serves.</summary>
    public const string EnvironmentSetting = "Environment";

    /// <summary>
    /// The environment a node serves when nothing says otherwise.
    /// </summary>
    /// <remarks>
    /// Matches what the shipped configuration files are named after, so a node with no
    /// settings at all behaves the way this one always did.
    /// </remarks>
    public const string DefaultEnvironment = "Production";

    /// <summary>The file the build ships, and the template every other name starts from.</summary>
    public const string TemplateFileName = "ocelot.json";

    private PublishedEnvironment(string configured)
    {
        Configured = configured;

        // The control plane's value object normalises to lower-case, and its
        // dictionaries are keyed on that. Spelled differently here, a snapshot
        // would be looked up under a key that never holds one.
        KeySegment = configured.Trim().ToLowerInvariant();
        FileBaseName = configured.Trim();
    }

    /// <summary>The environment exactly as it was configured.</summary>
    public string Configured { get; }

    /// <summary>The environment as it appears in a Redis key.</summary>
    public string KeySegment { get; }

    /// <summary>The environment as it appears in the file name Ocelot reads.</summary>
    public string FileBaseName { get; }

    /// <summary>The file Ocelot reads and the subscriber writes.</summary>
    public string ConfigurationFile => $"ocelot.{FileBaseName}.json";

    /// <summary>Where the published snapshot for this environment is stored.</summary>
    public string SnapshotKey(string version) => $"ocelot:snapshot:{KeySegment}:{version}";

    /// <summary>Where the current published version for this environment is stored.</summary>
    public string RuntimeCurrentKey() => $"ocelot:runtime:current:{KeySegment}";

    /// <summary>
    /// Whether a notification names this node's environment.
    /// </summary>
    /// <remarks>
    /// Compared rather than trusted: the control plane publishing production's snapshot
    /// to a development node is the cross-environment leak environment isolation exists
    /// to prevent. Case-insensitive, so either spelling is this environment and neither
    /// is another one.
    ///
    /// A notification that names no environment is accepted. The control plane does not
    /// always include one, and refusing it would leave every node unable to take a
    /// publication at all.
    /// </remarks>
    public bool Matches(string? named) =>
        string.IsNullOrWhiteSpace(named) ||
        string.Equals(named.Trim(), KeySegment, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(named.Trim(), FileBaseName, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// The environment a node serves, stated outright.
    /// </summary>
    /// <remarks>
    /// For a caller that already knows the name — a test hosting the pipeline, or a
    /// node whose environment comes from somewhere configuration cannot see. The
    /// same normalisation applies, so a name given here and a name read from
    /// configuration produce the same keys.
    /// </remarks>
    public static PublishedEnvironment For(string configured) => new(configured);

    /// <summary>
    /// Reads the node's environment from configuration, and falls back to the default.
    /// </summary>
    /// <remarks>
    /// Two spellings of the setting on purpose: <c>Gateway:Environment</c> for the
    /// appsettings an operator edits, and the environment variable for a container
    /// started with one line. A whitespace-only or empty value is treated as unset
    /// rather than as an environment name, because a node serving "" would look up
    /// keys nobody writes.
    /// </remarks>
    public static PublishedEnvironment FromConfiguration(IConfiguration configuration)
    {
        var configured = configuration[$"{SectionName}:{EnvironmentSetting}"]
            ?? configuration[EnvironmentVariable];

        return new PublishedEnvironment(
            string.IsNullOrWhiteSpace(configured) ? DefaultEnvironment : configured);
    }

    /// <summary>
    /// Makes sure the file this node's Ocelot reads exists, from the shipped template.
    /// </summary>
    /// <remarks>
    /// Ocelot loads its file during startup, before the subscriber can act on a
    /// publication, and it is registered as required — so a node configured for an
    /// environment whose file was never shipped would fail to start over a name. The
    /// template is copied once to the configured name, and the subscriber fills it
    /// afterwards. A node already serving an environment keeps its file: this only
    /// ever creates what is missing.
    /// </remarks>
    public static void EnsureConfigurationFile(string contentRoot, PublishedEnvironment environment)
    {
        var target = Path.Combine(contentRoot, environment.ConfigurationFile);
        if (File.Exists(target))
            return;

        var template = Path.Combine(contentRoot, TemplateFileName);
        if (!File.Exists(template))
            return;

        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        File.Copy(template, target);
    }
}
