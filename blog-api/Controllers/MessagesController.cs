using BlogApi.Data;
using BlogApi.Dtos;
using BlogApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BlogApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MessagesController(
    AppDbContext db,
    IConfiguration config,
    ILogger<MessagesController> logger) : ControllerBase
{
  [HttpPost]
  public async Task<ActionResult<MessageResponse>> Create(
      [FromBody] CreateMessageRequest request,
      CancellationToken cancellationToken)
  {
    var message = new Message
    {
      Name = request.Name.Trim(),
      Email = request.Email.Trim().ToLowerInvariant(),
      Content = request.Content.Trim(),
      CreatedAt = DateTime.UtcNow,
      IsRead = false,
    };

    db.Messages.Add(message);
    await db.SaveChangesAsync(cancellationToken);

    logger.LogInformation("New message from {Email}", message.Email);

    return CreatedAtAction(
        nameof(GetById),
        new { id = message.Id },
        ToResponse(message));
  }

  [HttpGet("{id:long}")]
  public async Task<ActionResult<MessageResponse>> GetById(long id, CancellationToken cancellationToken)
  {
    if (!IsAdmin()) return Unauthorized();

    var message = await db.Messages.AsNoTracking()
        .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

    return message is null ? NotFound() : ToResponse(message);
  }

  [HttpGet]
  public async Task<ActionResult<IEnumerable<MessageResponse>>> List(
      [FromQuery] int page = 1,
      [FromQuery] int pageSize = 20,
      CancellationToken cancellationToken = default)
  {
    if (!IsAdmin()) return Unauthorized();

    page = Math.Max(1, page);
    pageSize = Math.Clamp(pageSize, 1, 100);

    var items = await db.Messages.AsNoTracking()
        .OrderByDescending(m => m.CreatedAt)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync(cancellationToken);

    return Ok(items.Select(ToResponse));
  }

  private bool IsAdmin()
  {
    var expected = config["Admin:ApiKey"];
    if (string.IsNullOrWhiteSpace(expected)) return false;

    return Request.Headers.TryGetValue("X-Admin-Key", out var provided)
           && provided == expected;
  }

  private static MessageResponse ToResponse(Message m) =>
      new(m.Id, m.Name, m.Email, m.Content, m.CreatedAt, m.IsRead);
}
