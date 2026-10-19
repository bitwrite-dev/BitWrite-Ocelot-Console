using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using ocelot.gateway.node1.Configuration;
using Xunit;

namespace BitWrite.OcelotConsole.Gateway.Tests.Configuration;

/// <summary>
/// The environment a node serves, and the two names that follow from it.
/// </summary>
/// <remarks>
/// The file Ocelot reads and the Redis key the snapshot is read from both come from
/// this one setting, so a node that spelled them differently would serve one
/// environment's routes while claiming another's name — and report nothing. These
/// tests pin the pairing, the normalisation the control plane expects, and the
/// startup step that makes the file exist under the name configured.
/// </remarks>
public class PublishedEnvironmentTests
{
    private static IConfiguration Configuration(params (string Key, string Value)[] settings) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(settings.Select(s =>
                new KeyValuePair<string, string?>(s.Key, s.Value)))
            .Build();

    [Fact]
    public void DefaultsToWhatTheShippedFilesAreNamedAfter()
    {
        var environment = PublishedEnvironment.FromConfiguration(Configuration());

        // The file the build ships is ocelot.Production.json, so a node with no
        // settings behaves exactly the way it always did.
        environment.FileBaseName.Should().Be("Production");
        environment.ConfigurationFile.Should().Be("ocelot.Production.json");
        environment.SnapshotKey("7").Should().Be("ocelot:snapshot:production:7");
    }

    [Fact]
    public void ReadsTheEnvironmentFromConfiguration()
    {
        var environment = PublishedEnvironment.FromConfiguration(
            Configuration(("Gateway:Environment", "staging")));

        environment.FileBaseName.Should().Be("staging");
        environment.ConfigurationFile.Should().Be("ocelot.staging.json");
        environment.SnapshotKey("7").Should().Be("ocelot:snapshot:staging:7");
        environment.RuntimeCurrentKey().Should().Be("ocelot:runtime:current:staging");
    }

    [Fact]
    public void AlsoReadsTheEnvironmentVariableSpelling()
    {
        // A container is started with one line, and asking an operator to build a
        // configuration tree for that is a way to be ignored.
        var environment = PublishedEnvironment.FromConfiguration(
            Configuration(("GATEWAY_ENVIRONMENT", "development")));

        environment.KeySegment.Should().Be("development");
    }

    [Fact]
    public void TheKeyIsTheNormalisedNameTheControlPlaneUses()
    {
        // The control plane lower-cases environment names and keys its data on that,
        // so a node configured "Production" that looked up "Production" would find
        // nothing published and say so as if it were true.
        var environment = PublishedEnvironment.For("Production");

        environment.KeySegment.Should().Be("production");
        environment.FileBaseName.Should().Be("Production");
        environment.SnapshotKey("9").Should().Be("ocelot:snapshot:production:9");
    }

    [Fact]
    public void AnEmptySettingIsUnsetRatherThanAnEnvironment()
    {
        // A node serving "" would look up keys nobody writes and report nothing
        // published as though it were a fact.
        var environment = PublishedEnvironment.FromConfiguration(
            Configuration(("Gateway:Environment", "   ")));

        environment.FileBaseName.Should().Be("Production");
    }

    [Fact]
    public void AcceptsEitherSpellingOfItsOwnEnvironmentAndRefusesAnother()
    {
        var environment = PublishedEnvironment.For("Production");

        // The control plane serialises the name in different casings, and a node
        // that refused one spelling would ignore a publication of its own.
        environment.Matches("Production").Should().BeTrue();
        environment.Matches("production").Should().BeTrue();
        environment.Matches(null).Should().BeTrue();

        // Another environment's publication is refused — that refusal is the
        // cross-environment leak isolation exists to prevent.
        environment.Matches("staging").Should().BeFalse();
        environment.Matches("Staging").Should().BeFalse();
    }

    [Fact]
    public void CreatesTheConfiguredFileFromTheShippedTemplate()
    {
        // Ocelot reads the file as required at startup, so a node configured for an
        // environment whose file was never shipped would fail to start over a name.
        var directory = Directory.CreateTempSubdirectory("published-env-").FullName;
        try
        {
            File.WriteAllText(Path.Combine(directory, PublishedEnvironment.TemplateFileName), "{}");
            var environment = PublishedEnvironment.For("Development");

            PublishedEnvironment.EnsureConfigurationFile(directory, environment);

            File.Exists(Path.Combine(directory, "ocelot.Development.json")).Should().BeTrue();
            File.ReadAllText(Path.Combine(directory, "ocelot.Development.json")).Should().Be("{}");
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void LeavesAFileAlreadyThereAlone()
    {
        // The file holds the configuration the node is running; replacing it with the
        // empty template would blank a live gateway.
        var directory = Directory.CreateTempSubdirectory("published-env-").FullName;
        try
        {
            File.WriteAllText(Path.Combine(directory, PublishedEnvironment.TemplateFileName), "{}");
            File.WriteAllText(Path.Combine(directory, "ocelot.Production.json"), "{\"Routes\":[]}");
            var environment = PublishedEnvironment.For("Production");

            PublishedEnvironment.EnsureConfigurationFile(directory, environment);

            File.ReadAllText(Path.Combine(directory, "ocelot.Production.json"))
                .Should().Be("{\"Routes\":[]}");
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void AMissingTemplateLeavesTheStartupAloneRatherThanWritingNothing()
    {
        var directory = Directory.CreateTempSubdirectory("published-env-").FullName;
        try
        {
            var environment = PublishedEnvironment.For("Staging");

            // No exception: the node's own start-up then reports the missing file
            // where the operator can see it, rather than this inventing an empty
            // configuration that looks like a served one.
            PublishedEnvironment.EnsureConfigurationFile(directory, environment);

            File.Exists(Path.Combine(directory, "ocelot.Staging.json")).Should().BeFalse();
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }
}
