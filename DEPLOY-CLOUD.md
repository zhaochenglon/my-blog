# 云服务器部署指南（国内 · Docker 一键）

适合：已有 **腾讯云 / 阿里云轻量服务器**（Ubuntu 22.04），访客主要在国内。

---

## 最终架构

```
访客浏览器
    │
    ├─ https://blog.你的域名.cn     → Nginx 静态文件（blog 文件夹）
    │
    └─ https://api.你的域名.cn      → Nginx 反代 → Docker blog-api → Docker MySQL
```

也可继续用 **GitHub Pages** 放前端，只把 API + 数据库放云服务器。

---

## 第一步：准备云资源

| 项目 | 建议 |
|------|------|
| 服务器 | 轻量 2核2G，Ubuntu 22.04，地域选国内 |
| 域名 | `blog.xxx.cn` + `api.xxx.cn`（或同一域名不同路径） |
| 备案 | 服务器在中国大陆 + 自有域名 → 需 **ICP 备案** |
| 安全组 | 放行 **22**（SSH）、**80**、**443**；不要放行 3306 |

---

## 第二步：服务器安装 Docker

SSH 登录后执行：

```bash
curl -fsSL https://get.docker.com | sudo sh
sudo usermod -aG docker $USER
# 重新登录 SSH 使 docker 组生效
docker compose version
```

---

## 第三步：上传代码

**方式 A：Git（推荐）**

```bash
cd ~
git clone https://github.com/你的用户名/你的仓库.git blog-project
cd blog-project
```

**方式 B：本机打包上传**

在 Windows 项目根目录打包（不要含 bin/obj）后，用 WinSCP / `scp` 上传到服务器 `~/blog-project`。

---

## 第四步：配置生产环境变量

```bash
cd ~/blog-project
cp .env.production.example .env
nano .env
```

填写三项（示例）：

```env
MYSQL_ROOT_PASSWORD=你的强密码
ADMIN_API_KEY=随机长字符串至少32位
FRONTEND_ORIGIN=https://blog.你的域名.cn
```

若前端仍用 GitHub Pages：

```env
FRONTEND_ORIGIN=https://zhaochenglon.github.io
```

---

## 第五步：启动后端（Docker）

```bash
cd ~/blog-project
docker compose -f docker-compose.prod.yml up -d --build
docker compose -f docker-compose.prod.yml ps
```

确认 `blog-mysql` 为 healthy，`blog-api` 为 Up。

本机验证 API（在服务器上）：

```bash
curl -s http://127.0.0.1:5000/swagger/index.html | head
```

---

## 第六步：部署前端静态文件

```bash
sudo mkdir -p /var/www/blog
sudo cp -r ~/blog-project/blog/* /var/www/blog/
```

创建生产配置 `config.js`：

```bash
sudo nano /var/www/blog/js/config.js
```

内容：

```javascript
window.BLOG_CONFIG = {
  apiBaseUrl: "https://api.你的域名.cn",
};
```

---

## 第七步：安装 Nginx + HTTPS

```bash
sudo apt update
sudo apt install -y nginx certbot python3-certbot-nginx
sudo cp ~/blog-project/deploy/nginx/blog.conf.example /etc/nginx/sites-available/blog
sudo nano /etc/nginx/sites-available/blog
# 把所有「你的域名」改成真实域名

sudo ln -sf /etc/nginx/sites-available/blog /etc/nginx/sites-enabled/
sudo rm -f /etc/nginx/sites-enabled/default
sudo nginx -t
sudo systemctl reload nginx

sudo certbot --nginx -d blog.你的域名.cn -d api.你的域名.cn
```

按提示选择强制 HTTPS。

---

## 第八步：验证

| 检查项 | 地址 |
|--------|------|
| 博客首页 | https://blog.你的域名.cn |
| API 文档 | https://api.你的域名.cn/swagger |
| 提交留言 | 博客联系表单 |
| 查看留言 | Swagger Authorize + `GET /api/messages`（填 `.env` 里的 `ADMIN_API_KEY`） |

---

## 前端在 GitHub Pages 时的配置

1. 本地 `blog/js/config.js`（部署前生成并提交，或 CI 写入）：

```javascript
window.BLOG_CONFIG = {
  apiBaseUrl: "https://api.你的域名.cn",
};
```

2. 服务器 `.env` 中：

```env
FRONTEND_ORIGIN=https://zhaochenglon.github.io
```

3. 修改后重启 API：

```bash
docker compose -f docker-compose.prod.yml up -d --build blog-api
```

---

## 日常更新代码

```bash
cd ~/blog-project
git pull
docker compose -f docker-compose.prod.yml up -d --build
sudo cp -r blog/* /var/www/blog/
# 若改过 config.js，记得保留生产 api 地址
```

---

## 安全清单（上线前必做）

- [ ] 修改 `MYSQL_ROOT_PASSWORD`、`ADMIN_API_KEY`，不用开发默认值
- [ ] `.env` 不要提交到 GitHub
- [ ] MySQL 不映射公网端口（`docker-compose.prod.yml` 已处理）
- [ ] 仅 HTTPS 访问 API 与博客
- [ ] CORS `FRONTEND_ORIGIN` 与真实前端地址完全一致
- [ ] 定期备份：`docker exec blog-mysql mysqldump -uroot -p blog_db > backup.sql`

---

## 常见问题

**留言提交失败？**  
F12 看是否 CORS 或 Mixed Content（前端 https 调 http api）。生产必须全站 https。

**502 Bad Gateway？**  
`docker compose ps` 看 blog-api 是否运行；`curl http://127.0.0.1:5000/swagger`。

**备案未完成？**  
可先用服务器 **公网 IP** 测 API（安全组放行 80），域名备案通过后再绑域名与证书。

---

更细的 API 说明见 [DEPLOY.md](./DEPLOY.md)，本地 Docker 见 [DOCKER.md](./DOCKER.md)。
