using BlogApi.Models;
using Microsoft.EntityFrameworkCore;

namespace BlogApi.Data;

/// <summary>主库 / 从库共用的模型配置；迁移仅在 <see cref="AppDbContext"/> 上执行。</summary>
public abstract class BlogDbContextBase(DbContextOptions options) : DbContext(options)
{
  public DbSet<Message> Messages => Set<Message>();

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    modelBuilder.Entity<Message>(entity =>
    {
      entity.ToTable("messages");
      entity.HasKey(e => e.Id);
      entity.Property(e => e.Name).HasMaxLength(50).IsRequired();
      entity.Property(e => e.Email).HasMaxLength(100).IsRequired();
      entity.Property(e => e.Content).HasMaxLength(2000).IsRequired();
      entity.Property(e => e.CreatedAt).IsRequired();
      entity.HasIndex(e => e.CreatedAt);
      entity.HasIndex(e => e.Email);
    });
  }
}
