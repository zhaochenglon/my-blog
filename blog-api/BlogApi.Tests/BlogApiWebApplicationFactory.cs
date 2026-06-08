using BlogApi.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BlogApi.Tests;

public class BlogApiWebApplicationFactory : WebApplicationFactory<Program>
{
  public const string TestApiKey = "test-api-key";
  public const string TestUsername = "admin";
  public const string TestPassword = "test-password";

  protected override void ConfigureWebHost(IWebHostBuilder builder)
  {
    builder.UseEnvironment("Testing");

    builder.ConfigureAppConfiguration((_, config) =>
    {
      config.AddInMemoryCollection(new Dictionary<string, string?>
      {
        ["ConnectionStrings:Default"] = "Server=unused;Database=test;",
        ["ConnectionStrings:DefaultRead"] = "",
        ["ConnectionStrings:Redis"] = "",
        ["Admin:ApiKey"] = TestApiKey,
        ["Admin:Username"] = TestUsername,
        ["Admin:Password"] = TestPassword,
      });
    });

    builder.ConfigureServices(services =>
    {
      ReplaceDbContext<AppDbContext>(services, "BlogTestDb");
      ReplaceDbContext<AppReadDbContext>(services, "BlogTestDb");
    });
  }

  private static void ReplaceDbContext<TContext>(IServiceCollection services, string dbName)
      where TContext : DbContext
  {
    var descriptors = services
        .Where(d =>
            d.ServiceType == typeof(DbContextOptions<TContext>)
            || d.ServiceType == typeof(TContext))
        .ToList();

    foreach (var descriptor in descriptors)
      services.Remove(descriptor);

    services.AddDbContext<TContext>(options => options.UseInMemoryDatabase(dbName));
  }
}
