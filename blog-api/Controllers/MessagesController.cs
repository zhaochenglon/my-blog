using BlogApi.Data;
using BlogApi.Dtos;
using BlogApi.Models;
using BlogApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BlogApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MessagesController(
    AppDbContext db,
    AdminAuthService auth,
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
    if (!auth.IsAuthorized(Request)) return Unauthorized();

    var message = await db.Messages.AsNoTracking()
        .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

    return message is null ? NotFound() : ToResponse(message);
  }

  [HttpGet]
  public async Task<ActionResult<PagedMessageResponse>> List(
      [FromQuery] int page = 1,
      [FromQuery] int pageSize = 20,
      CancellationToken cancellationToken = default)
  {
    if (!auth.IsAuthorized(Request)) return Unauthorized();

    page = Math.Max(1, page);
    pageSize = Math.Clamp(pageSize, 1, 100);

    var query = db.Messages.AsNoTracking();
    var totalCount = await query.CountAsync(cancellationToken);

    var items = await query
        .OrderByDescending(m => m.CreatedAt)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync(cancellationToken);

    return Ok(new PagedMessageResponse(
        items.Select(ToResponse).ToList(),
        totalCount,
        page,
        pageSize));
  }

  [HttpDelete("{id:long}")]
  public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
  {
    if (!auth.IsAuthorized(Request)) return Unauthorized();

    var message = await db.Messages.FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
    if (message is null) return NotFound();

    db.Messages.Remove(message);
    await db.SaveChangesAsync(cancellationToken);

    logger.LogInformation("Deleted message {Id}", id);
    return NoContent();
  }

  private static MessageResponse ToResponse(Message m) =>
      new(m.Id, m.Name, m.Email, m.Content, AsUtc(m.CreatedAt), m.IsRead);

  private static DateTime AsUtc(DateTime value) =>
      value.Kind switch
      {
          DateTimeKind.Utc => value,
          DateTimeKind.Local => value.ToUniversalTime(),
          _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
      };
}
