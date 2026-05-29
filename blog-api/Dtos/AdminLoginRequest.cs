using System.ComponentModel.DataAnnotations;

namespace BlogApi.Dtos;

public class AdminLoginRequest
{
  [Required, MaxLength(50)]
  public string Username { get; set; } = string.Empty;

  [Required, MaxLength(100)]
  public string Password { get; set; } = string.Empty;
}
