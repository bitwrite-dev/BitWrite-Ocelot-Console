using System.Reflection;
using BitWrite.OcelotConsole.Api.Controllers;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace BitWrite.OcelotConsole.Api.Tests.Controllers;

/// <summary>
/// IAM-09: explicit authn/authz classification and justified anonymous exceptions for
/// IAM/security-sensitive API controllers (mapping/completion gated by P-GATE-1/2).
/// </summary>
public class IamSecurityApiAuthorizationContractTests
{
    [Fact]
    public void IdentityController_RequiresAuthenticationByDefault()
    {
        typeof(IdentityController).GetCustomAttributes<AuthorizeAttribute>(inherit: true)
            .Should().NotBeEmpty();
    }

    [Theory]
    [InlineData(nameof(IdentityController.CreateSession), false)]
    [InlineData(nameof(IdentityController.GetUserSessions), false)]
    [InlineData(nameof(IdentityController.RevokeSession), false)]
    [InlineData(nameof(IdentityController.Refresh), true)]
    [InlineData(nameof(IdentityController.Rotate), true)]
    [InlineData(nameof(IdentityController.RevokeRefreshToken), false)]
    public void IdentityController_Endpoints_HaveExplicitAnonymousClassification(string methodName, bool allowAnonymous)
    {
        var method = typeof(IdentityController).GetMethod(methodName)!;
        if (allowAnonymous)
            method.GetCustomAttributes<AllowAnonymousAttribute>().Should().ContainSingle();
        else
            method.GetCustomAttributes<AllowAnonymousAttribute>().Should().BeEmpty();
    }

    [Fact]
    public void IdentityController_HasNoDanglingPolicyReferences()
    {
        typeof(IdentityController).GetCustomAttributes<AuthorizeAttribute>(inherit: true)
            .Concat(typeof(IdentityController).GetMethods().SelectMany(m => m.GetCustomAttributes<AuthorizeAttribute>()))
            .Select(a => a.Policy)
            .Where(p => p is not null)
            .Should().BeEmpty("IAM endpoint permission mapping is gated (P-GATE-1/2); no dangling policy refs");
    }

    [Fact]
    public void AuditController_RequiresAuthentication()
    {
        typeof(AuditController).GetCustomAttributes<AuthorizeAttribute>(inherit: true)
            .Should().NotBeEmpty();
        typeof(AuditController).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .SelectMany(m => m.GetCustomAttributes<AllowAnonymousAttribute>())
            .Should().BeEmpty();
    }

    [Fact]
    public void BootstrapController_InitialAdmin_IsExplicitAnonymousException()
    {
        typeof(BootstrapController).GetCustomAttributes<AllowAnonymousAttribute>(inherit: true)
            .Should().BeEmpty();
        var method = typeof(BootstrapController).GetMethod(nameof(BootstrapController.BootstrapInitialAdmin))!;
        method.GetCustomAttributes<AllowAnonymousAttribute>().Should().ContainSingle(
            "bootstrap must be callable before any administrator/token exists");
        method.GetCustomAttributes<AuthorizeAttribute>().Should().BeEmpty();
    }
}
