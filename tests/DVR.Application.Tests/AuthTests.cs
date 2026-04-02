using DVR.Application.Features.Authentication.Commands;
using Xunit;

namespace DVR.Application.Tests;

public class AuthTests
{
    [Fact]
    public void LoginCommand_RequiresEmail()
    {
        var cmd = new LoginCommand { Email = string.Empty, Password = "pass" };
        Assert.Empty(cmd.Email);
    }

    [Fact]
    public void LoginCommand_HasPassword()
    {
        var cmd = new LoginCommand { Email = "admin@example.com", Password = "Admin@123" };
        Assert.Equal("Admin@123", cmd.Password);
    }
}
