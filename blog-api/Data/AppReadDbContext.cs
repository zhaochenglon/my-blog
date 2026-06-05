using Microsoft.EntityFrameworkCore;

namespace BlogApi.Data;

/// <summary>只读 DbContext，连接从库；仅用于查询，不执行迁移。</summary>
public class AppReadDbContext(DbContextOptions<AppReadDbContext> options) : BlogDbContextBase(options);
