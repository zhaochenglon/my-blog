using System.Net;
using System.Net.Http.Json;
using BlogApi.Dtos;

namespace BlogApi.Tests;

public class AdminApiTests : IClassFixture<BlogApiWebApplicationFactory>
{
  private readonly HttpClient _client;

  public AdminApiTests(BlogApiWebApplicationFactory factory)
  {
    _client = factory.CreateClient();
  }

  [Fact]
  public async Task Login_WithWrongPassword_ReturnsUnauthorized()
  {
    var response = await _client.PostAsJsonAsync("/api/admin/login", new AdminLoginRequest
    {
      Username = BlogApiWebApplicationFactory.TestUsername,
      Password = "wrong-password",
    });

    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
  }

  [Fact]
  public async Task Login_WithValidCredentials_ReturnsToken()
  {
    var response = await _client.PostAsJsonAsync("/api/admin/login", new AdminLoginRequest
    {
      Username = BlogApiWebApplicationFactory.TestUsername,
      Password = BlogApiWebApplicationFactory.TestPassword,
    });

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    var body = await response.Content.ReadFromJsonAsync<AdminLoginResponse>();
    Assert.NotNull(body);
    Assert.False(string.IsNullOrWhiteSpace(body.Token));
  }
}
