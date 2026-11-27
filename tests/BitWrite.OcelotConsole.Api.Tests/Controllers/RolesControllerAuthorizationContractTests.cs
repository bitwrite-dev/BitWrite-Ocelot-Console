using System.Reflection;
using BitWrite.OcelotConsole.Api.Controllers;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace BitWrite.OcelotConsole.Api.Tests.Controllers;

public class RolesControllerAuthorizationContractTests
{
    [Fact]
    public void RolesController_RequiresAuthentication()
    {
        typeof(RolesController).GetCustomAttributes<AuthorizeAttribute>(inherit: true)
            .Should().NotBeEmpty();
    }

    [Theory]
    [InlineData(nameof(RolesController.List), "perm:roles.read")]
    [InlineData(nameof(RolesController.Get), "perm:roles.read")]
    [InlineData(nameof(RolesController.Catalog), "perm:roles.read")]
    [InlineData(nameof(RolesController.Create), "perm:roles.manage")]
    [InlineData(nameof(RolesController.Update), "perm:roles.manage")]
    [InlineData(nameof(RolesController.Activate), "perm:roles.manage")]
    [InlineData(nameof(RolesController.Deactivate), "perm:roles.manage")]
    [InlineData(nameof(RolesController.AssignPermission), "perm:roles.manage")]
    [InlineData(nameof(RolesController.RemovePermission), "perm:roles.manage")]
    public void RolesController_Endpoints_HaveExpectedPermissionPolicy(string methodName, string policy)
    {
        var method = typeof(RolesController).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Single(m => m.Name == methodName);

        method.GetCustomAttributes<AllowAnonymousAttribute>().Should().BeEmpty();
        method.GetCustomAttributes<AuthorizeAttribute>()
            .Select(a => a.Policy)
            .Should().Contain(policy);
    }

    [Fact]
    public void RolesController_HasNoAnonymousEndpoints()
    {
        typeof(RolesController).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .SelectMany(m => m.GetCustomAttributes<AllowAnonymousAttribute>())
            .Should().BeEmpty();
    }
}
