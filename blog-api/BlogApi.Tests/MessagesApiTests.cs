using System.Net;
using System.Net.Http.Json;
using BlogApi.Dtos;

namespace BlogApi.Tests;

public class MessagesApiTests : IClassFixture<BlogApiWebApplicationFactory>
{
  private readonly HttpClient _client;

  public MessagesApiTests(BlogApiWebApplicationFactory factory)
  {
    _client = factory.CreateClient();
  }

  [Fact]
  public async Task CreateMessage_WithoutAuth_ReturnsCreated()
  {
    var request = new CreateMessageRequest
    {
      Name = "测试用户",
      Email = "test@example.com",
      Content = "这是一条测试留言",
    };

    var response = await _client.PostAsJsonAsync("/api/messages", request);

    Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    var body = await response.Content.ReadFromJsonAsync<MessageResponse>();
    Assert.NotNull(body);
    Assert.Equal("test@example.com", body.Email);
  }

  [Fact]
  public async Task ListMessages_WithoutAuth_ReturnsUnauthorized()
  {
    var response = await _client.GetAsync("/api/messages");

    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
  }

  [Fact]
  public async Task ListMessages_WithApiKey_ReturnsOk()
  {
    await _client.PostAsJsonAsync("/api/messages", new CreateMessageRequest
    {
      Name = "访客",
      Email = "guest@example.com",
      Content = "留言内容",
    });

    var request = new HttpRequestMessage(HttpMethod.Get, "/api/messages");
    request.Headers.Add("X-Admin-Key", BlogApiWebApplicationFactory.TestApiKey);

    var response = await _client.SendAsync(request);

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    var body = await response.Content.ReadFromJsonAsync<PagedMessageResponse>();
    Assert.NotNull(body);
    Assert.True(body.TotalCount >= 1);
  }
}
