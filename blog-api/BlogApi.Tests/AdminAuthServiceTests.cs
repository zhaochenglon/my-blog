using BlogApi.Services;
using Microsoft.Extensions.Configuration;

namespace BlogApi.Tests;

public class AdminAuthServiceTests
{
  private static AdminAuthService CreateService()
  {
    var config = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
          ["Admin:ApiKey"] = BlogApiWebApplicationFactory.TestApiKey,
          ["Admin:Username"] = BlogApiWebApplicationFactory.TestUsername,
          ["Admin:Password"] = BlogApiWebApplicationFactory.TestPassword,
        })
        .Build();

    return new AdminAuthService(config);
  }

  [Fact]
  public void CreateToken_ThenValidate_ReturnsTrue()
  {
    var auth = CreateService();
    var token = auth.CreateToken(BlogApiWebApplicationFactory.TestUsername);

    Assert.True(auth.TryValidateToken(token));
  }

  [Fact]
  public void TryValidateApiKey_WithWrongKey_ReturnsFalse()
  {
    var auth = CreateService();

    Assert.False(auth.TryValidateApiKey("wrong-key"));
    Assert.True(auth.TryValidateApiKey(BlogApiWebApplicationFactory.TestApiKey));
  }

  [Fact]
  public void TryValidateCredentials_WithWrongPassword_ReturnsFalse()
  {
    var auth = CreateService();

    Assert.False(auth.TryValidateCredentials("admin", "wrong"));
    Assert.True(auth.TryValidateCredentials(
        BlogApiWebApplicationFactory.TestUsername,
        BlogApiWebApplicationFactory.TestPassword));
  }
}
