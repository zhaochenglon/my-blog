using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace BlogApi.Services;

public class AdminAuthService(IConfiguration config)
{
  private static readonly JsonSerializerOptions JsonOptions = new()
  {
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
  };

  public bool TryValidateApiKey(string? apiKey)
  {
    var expected = config["Admin:ApiKey"];
    if (string.IsNullOrWhiteSpace(expected)) return false;
    return !string.IsNullOrWhiteSpace(apiKey) && apiKey == expected;
  }

  public bool TryValidateCredentials(string username, string password)
  {
    var expectedUser = config["Admin:Username"];
    var expectedPassword = config["Admin:Password"];
    if (string.IsNullOrWhiteSpace(expectedUser) || string.IsNullOrWhiteSpace(expectedPassword))
      return false;

    return username == expectedUser && password == expectedPassword;
  }

  public string CreateToken(string username)
  {
    var payload = new TokenPayload(username, DateTimeOffset.UtcNow.AddHours(8).ToUnixTimeSeconds());
    var payloadJson = JsonSerializer.Serialize(payload, JsonOptions);
    var payloadBytes = Encoding.UTF8.GetBytes(payloadJson);
    var payloadPart = Base64UrlEncode(payloadBytes);
    var signature = ComputeSignature(payloadPart);
    return $"{payloadPart}.{signature}";
  }

  public bool TryValidateToken(string? token)
  {
    if (string.IsNullOrWhiteSpace(token)) return false;

    var parts = token.Split('.');
    if (parts.Length != 2) return false;

    var expectedSignature = ComputeSignature(parts[0]);
    if (!CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expectedSignature),
            Encoding.UTF8.GetBytes(parts[1])))
      return false;

    TokenPayload? payload;
    try
    {
      var json = Encoding.UTF8.GetString(Base64UrlDecode(parts[0]));
      payload = JsonSerializer.Deserialize<TokenPayload>(json, JsonOptions);
    }
    catch
    {
      return false;
    }

    if (payload is null || string.IsNullOrWhiteSpace(payload.Username)) return false;
    if (payload.ExpiresAt < DateTimeOffset.UtcNow.ToUnixTimeSeconds()) return false;

    var expectedUser = config["Admin:Username"];
    return !string.IsNullOrWhiteSpace(expectedUser) && payload.Username == expectedUser;
  }

  public bool IsAuthorized(HttpRequest request)
  {
    if (request.Headers.TryGetValue("X-Admin-Key", out var apiKey)
        && TryValidateApiKey(apiKey))
      return true;

    var authHeader = request.Headers.Authorization.ToString();
    if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
    {
      var token = authHeader["Bearer ".Length..].Trim();
      return TryValidateToken(token);
    }

    return false;
  }

  private string ComputeSignature(string payloadPart)
  {
    var secret = config["Admin:ApiKey"];
    if (string.IsNullOrWhiteSpace(secret))
      secret = "fallback-secret-change-me";

    using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
    var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payloadPart));
    return Base64UrlEncode(hash);
  }

  private static string Base64UrlEncode(byte[] data) =>
      Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');

  private static byte[] Base64UrlDecode(string input)
  {
    var padded = input.Replace('-', '+').Replace('_', '/');
    switch (padded.Length % 4)
    {
      case 2: padded += "=="; break;
      case 3: padded += "="; break;
    }
    return Convert.FromBase64String(padded);
  }

  private sealed record TokenPayload(string Username, long ExpiresAt);
}
