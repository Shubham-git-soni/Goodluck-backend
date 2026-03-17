using DVR.Application.Features.Authentication.Commands;
using Xunit;

namespace DVR.Application.Tests;

public class AuthTests
{
    [Fact]
    public void LoginCommand_RequiresUsername()
    {
        var cmd = new LoginCommand { Username = string.Empty, Password = "pass" };
        Assert.Empty(cmd.Username);
    }

    [Fact]
    public void LoginCommand_HasPassword()
    {
        var cmd = new LoginCommand { Username = "admin", Password = "Admin@123" };
        Assert.Equal("Admin@123", cmd.Password);
    }
}
