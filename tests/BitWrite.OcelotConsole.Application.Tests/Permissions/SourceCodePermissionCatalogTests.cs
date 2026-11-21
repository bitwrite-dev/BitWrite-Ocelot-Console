using BitWrite.OcelotConsole.Application.Permissions;

namespace BitWrite.OcelotConsole.Application.Tests.Permissions;

public class SourceCodePermissionCatalogTests
{
    [Fact]
    public void Lookup_KnownActiveAssignable_ReturnsActiveAssignable()
    {
        var catalog = CreateCatalog(
            new PermissionDefinition("Catalog.Read", "Catalog Read", "Read catalog", "Catalog", PermissionStatus.Active, true));

        var result = catalog.Lookup("  CATALOG.READ  ");

        Assert.Equal(PermissionLookupState.Active, result.State);
        Assert.True(result.IsKnown);
        Assert.True(result.IsActive);
        Assert.False(result.IsRetired);
        Assert.False(result.IsUnknown);
        Assert.True(result.IsAssignable);
        Assert.True(result.CanGrant);
        Assert.NotNull(result.Definition);
        Assert.Equal("catalog.read", result.Definition!.Code);
        Assert.Equal("catalog.read", catalog.GetDefinition("Catalog.Read")!.Code);
        Assert.True(catalog.IsKnown("catalog.read"));
        Assert.True(catalog.IsActive("catalog.read"));
        Assert.True(catalog.IsAssignable("catalog.read"));
    }

    [Fact]
    public void Lookup_KnownRetired_ReturnsRetiredNotAssignableCannotGrant()
    {
        var catalog = CreateCatalog(
            new PermissionDefinition("catalog.read", "Catalog Read", null, null, PermissionStatus.Retired, true));

        var result = catalog.Lookup("CATALOG.READ");

        Assert.Equal(PermissionLookupState.Retired, result.State);
        Assert.True(result.IsKnown);
        Assert.False(result.IsActive);
        Assert.True(result.IsRetired);
        Assert.False(result.IsUnknown);
        Assert.False(result.IsAssignable);
        Assert.False(result.CanGrant);
        Assert.False(catalog.IsAssignable("catalog.read"));
        Assert.False(catalog.IsActive("catalog.read"));
    }

    [Fact]
    public void Lookup_ActiveNotAssignable_ReturnsActiveButNotAssignable()
    {
        var catalog = CreateCatalog(
            new PermissionDefinition("catalog.read", "Catalog Read", null, null, PermissionStatus.Active, false));

        var result = catalog.Lookup("catalog.read");

        Assert.True(result.IsActive);
        Assert.True(result.CanGrant);
        Assert.False(result.IsAssignable);
        Assert.False(catalog.IsAssignable("catalog.read"));
    }

    [Theory]
    [InlineData("unknown.code")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Lookup_UnknownOrEmpty_ReturnsUnknownNonGrantingNonAssignable(string? code)
    {
        var catalog = CreateCatalog(
            new PermissionDefinition("catalog.read", "Catalog Read", null, null, PermissionStatus.Active, true));

        var result = catalog.Lookup(code);

        Assert.Equal(PermissionLookupState.Unknown, result.State);
        Assert.False(result.IsKnown);
        Assert.False(result.IsActive);
        Assert.False(result.IsRetired);
        Assert.True(result.IsUnknown);
        Assert.False(result.IsAssignable);
        Assert.False(result.CanGrant);
        Assert.Null(result.Definition);
        Assert.Null(catalog.GetDefinition(code));
        Assert.False(catalog.IsKnown(code));
    }

    [Fact]
    public void GetAll_ReturnsNormalizedReadOnlyDefinitionsOrderedByCode()
    {
        var catalog = CreateCatalog(
            new PermissionDefinition(" Z.Write ", "Z", " d ", " c ", PermissionStatus.Active, true),
            new PermissionDefinition("a.read", "A", null, null, PermissionStatus.Retired, false));

        var all = catalog.GetAll();

        Assert.Equal(2, all.Count);
        Assert.Equal(new[] { "a.read", "z.write" }, all.Select(d => d.Code).ToArray());
        Assert.IsAssignableFrom<System.Collections.ObjectModel.ReadOnlyCollection<PermissionDefinition>>(all);
        Assert.Equal("Z", all.First(d => d.Code == "z.write").Name);
        Assert.Equal("d", all.First(d => d.Code == "z.write").Description);
        Assert.Equal("c", all.First(d => d.Code == "z.write").Category);
    }

    [Fact]
    public void Constructor_DuplicateNormalizedCodes_Throws()
    {
        var ex = Assert.Throws<ArgumentException>(() => CreateCatalog(
            new PermissionDefinition("catalog.read", "One", null, null, PermissionStatus.Active, true),
            new PermissionDefinition(" CATALOG.READ ", "Two", null, null, PermissionStatus.Retired, true)));

        Assert.Contains("Duplicate permission code", ex.Message);
    }

    [Fact]
    public void Constructor_EmptyCode_Throws()
    {
        var ex = Assert.Throws<ArgumentException>(() => CreateCatalog(
            new PermissionDefinition("   ", "Name", null, null, PermissionStatus.Active, true)));

        Assert.Contains("non-empty code", ex.Message);
    }

    [Fact]
    public void DefaultDefinitionsRegistry_IsSourceCodeBackedWithApprovedCodes()
    {
        var all = BitWritePermissionCatalogDefinitions.All;

        Assert.NotNull(all);
        Assert.NotEmpty(all);

        // Core IAM permissions present
        Assert.Contains(all, d => d.Code == "perm.users.read");
        Assert.Contains(all, d => d.Code == "perm.users.manage");
        Assert.Contains(all, d => d.Code == "perm.roles.read");
        Assert.Contains(all, d => d.Code == "perm.roles.manage");
        Assert.Contains(all, d => d.Code == "perm.permissions.read");
        Assert.Contains(all, d => d.Code == "perm.permissions.manage");
        Assert.Contains(all, d => d.Code == "perm.sessions.read");
        Assert.Contains(all, d => d.Code == "perm.sessions.manage");
        Assert.Contains(all, d => d.Code == "perm.tokens.manage");
        Assert.Contains(all, d => d.Code == "perm.audit.read");
        Assert.Contains(all, d => d.Code == "perm.bootstrap.admin");

        // All codes are Active and Assignable
        Assert.All(all, d =>
        {
            Assert.Equal(PermissionStatus.Active, d.Status);
            Assert.True(d.Assignable);
        });

        var catalog = new SourceCodePermissionCatalog(all);
        Assert.Equal(all.Count, catalog.GetAll().Count());
        Assert.True(catalog.Lookup("perm.users.read").IsActive);
        Assert.True(catalog.Lookup("any.permission").IsUnknown);
    }

    private static SourceCodePermissionCatalog CreateCatalog(params PermissionDefinition[] definitions) =>
        new(definitions);
}
