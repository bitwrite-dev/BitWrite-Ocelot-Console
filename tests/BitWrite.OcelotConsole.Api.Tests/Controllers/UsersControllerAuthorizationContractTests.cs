using System.Reflection;
using BitWrite.OcelotConsole.Api.Controllers;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace BitWrite.OcelotConsole.Api.Tests.Controllers;

public class UsersControllerAuthorizationContractTests
{
    [Fact]
    public void UsersController_RequiresAuthentication()
    {
        typeof(UsersController).GetCustomAttributes<AuthorizeAttribute>(inherit: true)
            .Should().NotBeEmpty();
    }

    [Fact]
    public void UsersController_HasNoAnonymousEndpoints_AndNoDanglingPolicyRefs()
    {
        var methods = typeof(UsersController).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        methods.SelectMany(m => m.GetCustomAttributes<AllowAnonymousAttribute>()).Should().BeEmpty();
        typeof(UsersController).GetCustomAttributes<AuthorizeAttribute>(inherit: true)
            .Concat(methods.SelectMany(m => m.GetCustomAttributes<AuthorizeAttribute>()))
            .Select(a => a.Policy)
            .Where(p => p is not null)
            .Should().BeEmpty("endpoint-to-permission mapping is gated (P-GATE-1/2)");
    }
}
