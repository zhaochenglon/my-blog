# Docker 本地开发指南

使用 Docker 运行 **MySQL + 留言 API**，无需在本机安装 MySQL。

## 前置条件

- 已安装 [Docker Desktop](https://www.docker.com/products/docker-desktop/)（Windows）
- 安装后确认：

```powershell
docker --version
docker compose version
```

---

## 方式一：一键启动 MySQL + API（推荐）

### 1. 配置环境变量（可选）

```powershell
cd d:\Cursor\20260527demo
copy .env.example .env
```

默认密码为 `blog_dev_password`，可在 `.env` 中修改 `MYSQL_ROOT_PASSWORD`。

### 2. 启动全部服务

```powershell
docker compose up -d --build
```

首次会拉取镜像并构建 API，约 1～3 分钟。

### 3. 查看状态

```powershell
docker compose ps
docker compose logs -f blog-api
```

### 4. 访问

| 服务 | 地址 |
|------|------|
| API / Swagger | http://localhost:5059/swagger |
| 提交留言 | `POST http://localhost:5059/api/messages` |
| MySQL | `localhost:3306`（用户 `root`，库 `blog_db`） |

### 5. 前端联调

确认 `blog/js/config.js`：

```javascript
window.BLOG_CONFIG = {
  apiBaseUrl: "http://localhost:5059",
};
```

用 **Live Server** 或 `npx serve -l 5500` 打开 `blog/index.html`（地址需为 `http://`，不要用 `file://`）。

若浏览器地址是 `http://172.18.x.x:5500` 这类局域网 IP，开发环境 API 已允许；仍失败时请改用 http://localhost:5500 。

### 6. 停止与清理

```powershell
# 停止容器（保留数据）
docker compose down

# 停止并删除数据库卷（清空留言数据）
docker compose down -v
```

---

## 方式二：只跑 MySQL，API 用 dotnet run

适合需要断点调试 C# 代码时。

### 1. 启动 MySQL

```powershell
docker compose -f docker-compose.db-only.yml up -d
```

### 2. 修改连接字符串

`blog-api/appsettings.Development.json`：

```json
"ConnectionStrings": {
  "Default": "Server=127.0.0.1;Port=3306;Database=blog_db;User=root;Password=blog_dev_password;"
}
```

密码需与 `.env` 中 `MYSQL_ROOT_PASSWORD` 一致（默认 `blog_dev_password`）。

### 3. 本机启动 API

```powershell
cd blog-api
dotnet run
```

---

## 常用命令

```powershell
# 重新构建 API 镜像（改代码后）
docker compose up -d --build blog-api

# 进入 MySQL 命令行
docker exec -it blog-mysql mysql -uroot -p

# 查看 API 日志
docker compose logs -f blog-api
```

---

## 查看留言（管理接口）

```http
GET http://localhost:5059/api/messages
X-Admin-Key: dev-admin-key-change-in-production
```

---

## 常见问题

**Q: 端口 3306 已被占用？**  
修改 `docker-compose.yml` 中 `ports` 为 `"3307:3306"`，连接字符串端口改为 `3307`。

**Q: API 启动报错连不上数据库？**  
等 MySQL 健康检查通过：`docker compose ps` 中 mysql 状态为 `healthy` 后再看 API 日志。

**Q: 改了 C# 代码如何生效？**  
```powershell
docker compose up -d --build blog-api
```

**Q: 生产环境能用这份 compose 吗？**  
仅供本地开发。生产请按 [DEPLOY.md](./DEPLOY.md) 部署到腾讯云，并修改密码与 `Admin:ApiKey`。
