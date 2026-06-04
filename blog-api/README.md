# BlogApi — 博客留言 API

ASP.NET Core 8 Web API，使用 Entity Framework Core + MySQL 存储访客留言。

## 快速开始

```powershell
# 1. MySQL 中创建库
# CREATE DATABASE blog_db CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

# 2. 修改 appsettings.Development.json 中的连接字符串

# 3. 启动
dotnet run

# 4. Swagger
# http://localhost:5059/swagger
```

## Redis 缓存（留言列表）

- 仅 **`GET /api/messages`**（管理员列表）走 Redis，TTL 默认 **10 分钟**（`Redis:MessageListCacheMinutes`）。
- 模式：**Cache-Aside** — 先读缓存，未命中查 MySQL 再写入；`POST` / `DELETE` 后递增版本号使列表缓存失效。
- 实现：`Services/MessageListCacheService.cs`；Docker 需启动 `redis` 服务（见根目录 `docker-compose.yml`）。

```bash
# 观察缓存 key（开发）
docker exec -it blog-redis redis-cli KEYS 'blog:*'
```

## 接口

- `POST /api/messages` — 提交留言（公开）
- `GET /api/messages` — 列表（请求头 `X-Admin-Key`）
- `GET /api/messages/{id}` — 详情（请求头 `X-Admin-Key`）

完整部署说明见项目根目录 [DEPLOY.md](../DEPLOY.md)。
