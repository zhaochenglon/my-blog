# 博客留言 API 部署指南（C# + MySQL + 腾讯云）

本文说明如何将 **ASP.NET Core 留言 API** 部署到国内云，并与静态前端（GitHub Pages / 腾讯云 COS）联调。

---

## 架构一览

| 组件 | 技术 | 部署位置 |
|------|------|----------|
| 前端 | HTML / CSS / JS | GitHub Pages 或 腾讯云 COS 静态网站 |
| 后端 API | ASP.NET Core 8 (`blog-api`) | 腾讯云轻量服务器 / 云托管 |
| 数据库 | MySQL 8 | 同机 MySQL 或 腾讯云云数据库 |

---

## 一、本机环境说明

本机已安装 **.NET SDK 9.0**（可编译 **.NET 8** 的 `blog-api` 项目）。

检查命令：

```powershell
dotnet --version
dotnet --list-sdks
```

---

## 二、本地开发联调

### 推荐：Docker 一键启动（无需本机安装 MySQL）

详见 **[DOCKER.md](./DOCKER.md)**：

```powershell
cd d:\Cursor\20260527demo
copy .env.example .env
docker compose up -d --build
```

API：`http://localhost:5059/swagger`

### 备选：本机 MySQL + dotnet run

创建数据库后，修改 `blog-api/appsettings.Development.json` 连接字符串，再执行 `dotnet run`。

### 4. 配置前端

```powershell
copy blog\js\config.example.js blog\js\config.js
```

确认 `blog/js/config.js` 中：

```javascript
window.BLOG_CONFIG = {
  apiBaseUrl: "http://localhost:5059",
};
```

### 5. 用本地服务器打开前端（不要直接双击 HTML）

CORS 要求页面来源为 `http://localhost:xxxx`，例如 VS Code **Live Server**（5500 端口已在开发环境 CORS 白名单中）。

在 `blog/index.html` 联系表单提交留言，成功后会 Toast 提示。

> 若 GitHub Pages 的站点根目录是 `blog` 文件夹，请在仓库 Settings → Pages 中将路径设为 `/blog`，或把 `blog` 内文件放到仓库根目录。

### 6. 查看留言（管理接口）

```http
GET http://localhost:5059/api/messages
X-Admin-Key: dev-admin-key-change-in-production
```

生产环境务必修改 `Admin:ApiKey` 为长随机字符串。

---

## 三、腾讯云部署 API

### 1. 准备资源

- **轻量应用服务器**（Ubuntu 22.04，2核2G 起）
- **MySQL**：同机安装，或购买 **云数据库 MySQL**（推荐，自动备份）

### 2. 服务器安装运行时

```bash
# Ubuntu
wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
sudo apt update
sudo apt install -y aspnetcore-runtime-8.0 nginx
```

### 3. 发布项目（在开发机执行）

```powershell
cd d:\Cursor\20260527demo\blog-api
dotnet publish -c Release -o ./publish
```

将 `publish` 文件夹上传到服务器，例如 `/var/www/blog-api`。

### 4. 生产配置

在服务器创建 `/var/www/blog-api/appsettings.Production.json`（或通过环境变量）：

```json
{
  "ConnectionStrings": {
    "Default": "Server=内网地址;Port=3306;Database=blog_db;User=blog;Password=强密码;"
  },
  "Cors": {
    "AllowedOrigins": [
      "https://zhaochenglon.github.io",
      "https://你的自定义域名.cn"
    ]
  },
  "Admin": {
    "ApiKey": "生产环境长随机密钥"
  }
}
```

环境变量示例：

```bash
export ASPNETCORE_ENVIRONMENT=Production
export ConnectionStrings__Default="Server=..."
export Admin__ApiKey="..."
```

### 5. systemd 守护进程

`/etc/systemd/system/blog-api.service`：

```ini
[Unit]
Description=Blog Message API
After=network.target

[Service]
WorkingDirectory=/var/www/blog-api
ExecStart=/usr/bin/dotnet /var/www/blog-api/BlogApi.dll
Restart=always
RestartSec=5
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://127.0.0.1:5000

[Install]
WantedBy=multi-user.target
```

```bash
sudo systemctl daemon-reload
sudo systemctl enable blog-api
sudo systemctl start blog-api
```

首次启动会自动执行数据库迁移（`Program.cs` 中的 `Migrate()`）。

### 6. Nginx 反向代理 + HTTPS

`/etc/nginx/sites-available/blog-api`：

```nginx
server {
    listen 80;
    server_name api.你的域名.cn;

    location / {
        proxy_pass http://127.0.0.1:5000;
        proxy_http_version 1.1;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

启用站点后，用腾讯云控制台申请 **免费 SSL**，或 `certbot` 配置 HTTPS。

### 7. 防火墙

仅开放 **80 / 443**；MySQL **不要**对公网开放。

---

## 四、前端对接生产 API

部署前端前，在构建/上传前设置 `blog/js/config.js`（或通过 CI 注入）：

```javascript
window.BLOG_CONFIG = {
  apiBaseUrl: "https://api.你的域名.cn",
};
```

> `config.js` 已在 `.gitignore` 中。GitHub Pages 部署时，可在仓库保留 `config.example.js`，在 Pages 构建步骤生成 `config.js`，或把生产地址写入 `config.js` 后单独上传。

**务必**在 API 的 `Cors:AllowedOrigins` 中加入你的前端完整来源（含 `https://`，无末尾斜杠）。

---

## 五、域名与备案（国内）

- 服务器在**中国大陆**且使用自有域名 → 需 **ICP 备案**
- 建议：`blog.xxx.cn` → 静态站，`api.xxx.cn` → 本 API

---

## 六、安全清单

- [ ] 修改 `Admin:ApiKey`，勿使用默认值
- [ ] 生产使用 HTTPS
- [ ] MySQL 使用独立账号、强密码，仅内网访问
- [ ] CORS 仅允许你的前端域名
- [ ] 定期备份 `messages` 表
- [ ] 后续可加：验证码、IP 限流（Nginx `limit_req`）

---

## 七、API 接口说明

| 方法 | 路径 | 权限 | 说明 |
|------|------|------|------|
| POST | `/api/messages` | 公开 | 提交留言，Body: `{ name, email, content }` |
| GET | `/api/messages` | 需 `X-Admin-Key` | 分页列表 |
| GET | `/api/messages/{id}` | 需 `X-Admin-Key` | 单条详情 |

---

## 八、常见问题

**Q: 前端提示「无法连接」？**  
检查 API 是否启动、CORS 是否包含前端来源、`config.js` 的 `apiBaseUrl` 是否正确。

**Q: 双击打开 index.html 提交失败？**  
请用 `http://localhost` 的本地服务器打开页面。

**Q: 数据库迁移失败？**  
确认连接字符串、MySQL 已启动、`blog_db` 已创建，查看 `journalctl -u blog-api -f` 日志。
