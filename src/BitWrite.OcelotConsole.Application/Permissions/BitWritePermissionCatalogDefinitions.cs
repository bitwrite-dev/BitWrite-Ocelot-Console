using BitWrite.OcelotConsole.Application.Permissions;

namespace BitWrite.OcelotConsole.Application.Permissions;

/// <summary>
/// Product-owned, source-code Permission Catalog definitions (SoT).
/// </summary>
/// <remarks>
/// Approved production permission codes. Read-only at runtime.
/// </remarks>
public static class BitWritePermissionCatalogDefinitions
{
    public static IReadOnlyCollection<PermissionDefinition> All { get; } = new[]
    {
        // Users
        new PermissionDefinition("perm.users.read", "Read Users", "View user accounts", "Users", PermissionStatus.Active, true),
        new PermissionDefinition("perm.users.manage", "Manage Users", "Create, update, enable, disable users", "Users", PermissionStatus.Active, true),

        // Roles
        new PermissionDefinition("perm.roles.read", "Read Roles", "View roles", "Roles", PermissionStatus.Active, true),
        new PermissionDefinition("perm.roles.manage", "Manage Roles", "Create, update, activate, deactivate roles", "Roles", PermissionStatus.Active, true),

        // Permissions
        new PermissionDefinition("perm.permissions.read", "Read Permissions", "View permission catalog", "Permissions", PermissionStatus.Active, true),
        new PermissionDefinition("perm.permissions.manage", "Manage Permissions", "Assign/remove permissions from roles", "Permissions", PermissionStatus.Active, true),

        // Sessions
        new PermissionDefinition("perm.sessions.read", "Read Sessions", "View active sessions", "Sessions", PermissionStatus.Active, true),
        new PermissionDefinition("perm.sessions.manage", "Manage Sessions", "Revoke sessions", "Sessions", PermissionStatus.Active, true),

        // Tokens
        new PermissionDefinition("perm.tokens.manage", "Manage Tokens", "Revoke refresh tokens", "Tokens", PermissionStatus.Active, true),

        // Services
        new PermissionDefinition("perm.services.read", "Read Services", "View services", "Services", PermissionStatus.Active, true),
        new PermissionDefinition("perm.services.manage", "Manage Services", "Configure services", "Services", PermissionStatus.Active, true),

        // Routes
        new PermissionDefinition("perm.routes.read", "Read Routes", "View routes", "Routes", PermissionStatus.Active, true),
        new PermissionDefinition("perm.routes.manage", "Manage Routes", "Configure routes", "Routes", PermissionStatus.Active, true),

        // Global Configuration
        new PermissionDefinition("perm.globalconfig.read", "Read Global Configuration", "View global configuration", "GlobalConfiguration", PermissionStatus.Active, true),
        new PermissionDefinition("perm.globalconfig.manage", "Manage Global Configuration", "Configure global settings", "GlobalConfiguration", PermissionStatus.Active, true),

        // Snapshots
        new PermissionDefinition("perm.snapshots.read", "Read Snapshots", "View snapshots", "Snapshots", PermissionStatus.Active, true),
        new PermissionDefinition("perm.snapshots.manage", "Manage Snapshots", "Create/publish snapshots", "Snapshots", PermissionStatus.Active, true),

        // Publications
        new PermissionDefinition("perm.publications.read", "Read Publications", "View publications", "Publications", PermissionStatus.Active, true),
        new PermissionDefinition("perm.publications.manage", "Manage Publications", "Create/publish publications", "Publications", PermissionStatus.Active, true),

        // Gateways
        new PermissionDefinition("perm.gateways.read", "Read Gateways", "View gateways", "Gateways", PermissionStatus.Active, true),
        new PermissionDefinition("perm.gateways.manage", "Manage Gateways", "Configure gateways", "Gateways", PermissionStatus.Active, true),

        // Runtime
        new PermissionDefinition("perm.runtime.read", "Read Runtime", "View runtime state", "Runtime", PermissionStatus.Active, true),
        new PermissionDefinition("perm.runtime.manage", "Manage Runtime", "Configure runtime", "Runtime", PermissionStatus.Active, true),

        // Plugins
        new PermissionDefinition("perm.plugins.read", "Read Plugins", "View plugins", "Plugins", PermissionStatus.Active, true),
        new PermissionDefinition("perm.plugins.manage", "Manage Plugins", "Install/configure plugins", "Plugins", PermissionStatus.Active, true),

        // Licenses
        new PermissionDefinition("perm.licenses.read", "Read Licenses", "View licenses", "Licenses", PermissionStatus.Active, true),
        new PermissionDefinition("perm.licenses.manage", "Manage Licenses", "Configure licenses", "Licenses", PermissionStatus.Active, true),

        // Settings
        new PermissionDefinition("perm.settings.read", "Read Settings", "View settings", "Settings", PermissionStatus.Active, true),
        new PermissionDefinition("perm.settings.manage", "Manage Settings", "Configure settings", "Settings", PermissionStatus.Active, true),

        // Audit
        new PermissionDefinition("perm.audit.read", "Read Audit", "View audit log", "Audit", PermissionStatus.Active, true),

        // Bootstrap — anonymous exception (callable before admin exists)
        new PermissionDefinition("perm.bootstrap.admin", "Bootstrap Admin", "Run initial admin bootstrap", "Bootstrap", PermissionStatus.Active, true),
    };
}
