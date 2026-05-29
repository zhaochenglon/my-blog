using BlogApi.Dtos;
using BlogApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace BlogApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdminController(AdminAuthService auth) : ControllerBase
{
  [HttpPost("login")]
  public ActionResult<AdminLoginResponse> Login([FromBody] AdminLoginRequest request)
  {
    if (!auth.TryValidateCredentials(request.Username.Trim(), request.Password))
      return Unauthorized(new { message = "账号或密码错误" });

    var expiresAt = DateTime.UtcNow.AddHours(8);
    var token = auth.CreateToken(request.Username.Trim());
    return Ok(new AdminLoginResponse(token, expiresAt));
  }
}
