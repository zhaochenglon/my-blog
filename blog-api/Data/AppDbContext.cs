using Microsoft.EntityFrameworkCore;

namespace BlogApi.Data;

/// <summary>读写 DbContext，连接主库；增删改与 EF 迁移均使用此上下文。</summary>
public class AppDbContext(DbContextOptions<AppDbContext> options) : BlogDbContextBase(options);
