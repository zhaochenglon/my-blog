using BlogApi.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Connection string 'Default' is not configured.");

var serverVersion = ServerVersion.Parse("8.0.36-mysql");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, serverVersion));

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:5500", "http://127.0.0.1:5500", "http://localhost:3000"];
var isDevelopment = builder.Environment.IsDevelopment();

builder.Services.AddCors(options =>
{
  options.AddPolicy("BlogFrontend", policy =>
  {
    if (isDevelopment)
    {
      // 开发环境：允许 Live Server / serve 在 localhost、127.0.0.1 或局域网 IP（如 172.18.x.x）访问
      policy.SetIsOriginAllowed(origin =>
      {
        if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri))
          return false;

        var devPorts = new[] { 5500, 8080, 3000, 5173 };
        if (!devPorts.Contains(uri.Port))
          return false;

        if (uri.Host is "localhost" or "127.0.0.1")
          return true;

        if (System.Net.IPAddress.TryParse(uri.Host, out var ip))
        {
          var bytes = ip.GetAddressBytes();
          if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
          {
            return bytes[0] == 10
                || (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
                || (bytes[0] == 192 && bytes[1] == 168);
          }
        }

        return false;
      });
    }
    else
    {
      policy.WithOrigins(allowedOrigins);
    }

    policy.AllowAnyHeader().AllowAnyMethod();
  });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
  options.AddSecurityDefinition("AdminApiKey", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
  {
    Name = "X-Admin-Key",
    Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
    In = Microsoft.OpenApi.Models.ParameterLocation.Header,
    Description = "管理接口密钥，与 Admin:ApiKey 配置一致（Docker 默认见 appsettings.Development.json）",
  });
  options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
  {
    {
      new Microsoft.OpenApi.Models.OpenApiSecurityScheme
      {
        Reference = new Microsoft.OpenApi.Models.OpenApiReference
        {
          Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
          Id = "AdminApiKey",
        },
      },
      Array.Empty<string>()
    },
  });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
  var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
  var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
  try
  {
    var pending = db.Database.GetPendingMigrations().ToList();
    if (pending.Count > 0)
    {
      logger.LogInformation("Applying migrations: {Migrations}", string.Join(", ", pending));
    }
    db.Database.Migrate();
    logger.LogInformation("Database migrations applied.");
  }
  catch (Exception ex)
  {
    logger.LogError(ex, "Database migration failed.");
    throw;
  }
}

if (app.Environment.IsDevelopment())
{
  app.UseSwagger();
  app.UseSwaggerUI();
}

app.UseCors("BlogFrontend");

if (!app.Environment.IsDevelopment())
{
  app.UseHttpsRedirection();
}

app.MapControllers();

app.Run();
