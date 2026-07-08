# MySQL 一主一从 + EF Core 读写分离

> 轻量服务器自建双 MySQL，binlog/GTID 异步复制；API **写主库、读从库**（含 Redis 未命中时）。

---

## 一、架构

```
                    Nginx
                      │
            ┌─────────┴─────────┐
            ▼                   ▼
        blog-api-1          blog-api-2
            │                   │
     写 ────┼───────────────────┼──── Default → mysql-master
     读 ────┼───────────────────┼──── DefaultRead → mysql-slave
            │                   │
            └─────────┬─────────┘
                      ▼
                 blog-redis
                      │
        ┌─────────────┴─────────────┐
        ▼                           ▼
  mysql-master (主)  ──binlog/GTID──►  mysql-slave (从, read_only)
```

| 组件 | 职责 |
|------|------|
| `AppDbContext` | 连接主库：`POST` / `DELETE`、EF `Migrate()` |
| `AppReadDbContext` | 连接从库：`GET` 列表（cache miss）、`GET` 详情 |
| `MessageListCacheService` | 不变；miss 时由 `readDb` 查从库并回填 Redis |

**未配置 `DefaultRead` 时**：读连接回退到主库（与单库 `docker-compose.prod.yml` 兼容）。

---

## 二、复制原理（无固定「同步间隔」）

1. 主库提交事务 → 写入 **binlog**（`log_bin=ON`，`binlog_format=ROW`）。
2. 从库 **IO 线程** 连接主库，拉取 binlog 写入 **relay log**。
3. 从库 **SQL 线程** 重放 relay log，数据与主库 **近实时** 一致（通常 &lt;1 秒）。
4. 使用 **GTID**（`SOURCE_AUTO_POSITION=1`），从库无需手填 binlog 文件名。

配置文件：

- 主库：`deploy/mysql/master.cnf`（`server-id=1`）
- 从库：`deploy/mysql/slave.cnf`（`server-id=2`，`read_only`）

---

## 三、代码结构（EF Core 双 DbContext）

| 文件 | 说明 |
|------|------|
| `Data/BlogDbContextBase.cs` | 共用 `Message` 映射 |
| `Data/AppDbContext.cs` | 主库，迁移 |
| `Data/AppReadDbContext.cs` | 从库，只读查询 |
| `Program.cs` | 注册两个 `UseMySql`；仅 `AppDbContext.Database.Migrate()` |
| `MessagesController.cs` | 写 `db`，读 `readDb` |

连接串（Docker 环境变量）：

```text
ConnectionStrings__Default=Server=mysql-master;...
ConnectionStrings__DefaultRead=Server=mysql-slave;...
```

---

## 四、服务器部署步骤

### 4.1 从单库迁移到主从（会新建主/从数据卷）

若当前使用 `docker-compose.prod.yml` 单容器 `blog-mysql`，切主从前请 **备份**：

```bash
sudo docker exec blog-mysql mysqldump -uroot -p blog_db > blog_db_backup.sql
```

### 4.2 停掉旧栈（可选）

```bash
cd ~/blog-project
sudo docker compose -f docker-compose.prod.yml down
# 旧卷 blog_mysql_data 保留不动；主从使用新卷 blog_mysql_master_data / blog_mysql_slave_data
```

### 4.3 启动主库 + 从库 + Redis

```bash
# .env 中需有 MYSQL_ROOT_PASSWORD、ADMIN_*、FRONTEND_ORIGIN 等
echo 'MYSQL_REPLICATION_PASSWORD=你的repl密码' >> .env
# repl 密码须 ≤32 字符（MySQL 限制；勿用带连字符的 UUID，可用 32 位 hex）

sudo docker compose -f docker-compose.replica.yml up -d mysql-master mysql-slave redis
```

等待健康：

```bash
sudo docker compose -f docker-compose.replica.yml ps
```

### 4.4 启动 API，在主库执行迁移

```bash
sudo docker compose -f docker-compose.replica.yml up -d --build blog-api-1
sudo docker compose -f docker-compose.replica.yml logs blog-api-1 | tail -20
# 应看到 Database migrations applied. 与 Read DB: using replica connection
```

若有旧数据备份，导入 **主库**：

```bash
sudo docker exec -i blog-mysql-master mysql -uroot -p blog_db < blog_db_backup.sql
```

### 4.5 初始化复制（只需一次）

```bash
chmod +x deploy/mysql/setup-replication.sh
export MYSQL_ROOT_PASSWORD='与.env一致'
export MYSQL_REPLICATION_PASSWORD='与.env中repl密码一致'
bash deploy/mysql/setup-replication.sh
```

确认输出中：

- `Replica_IO_Running: Yes`
- `Replica_SQL_Running: Yes`
- `Seconds_Behind_Source: 0`（或很小）

### 4.6 启动第二个 API

```bash
sudo docker compose -f docker-compose.replica.yml up -d blog-api-2
```

Nginx `upstream` 仍指向 5001/5002，无需因主从而改端口。

---

## 五、验证

### 5.1 复制是否正常

```bash
# 主库插入测试行后，从库应能查到（延迟极短）
sudo docker exec -it blog-mysql-master mysql -uroot -p blog_db \
  -e "SELECT COUNT(*) AS master_count FROM messages;"
sudo docker exec -it blog-mysql-slave mysql -uroot -p blog_db \
  -e "SELECT COUNT(*) AS slave_count FROM messages;"
```

### 5.2 API 读写分离

```bash
# 读列表（走从库 + 可能 Redis）
curl -s -H "X-Admin-Key: 你的密钥" \
  "http://127.0.0.1:5001/api/messages?page=1&pageSize=5"
```

日志：

```bash
sudo docker compose -f docker-compose.replica.yml logs blog-api-1 | grep -i "Read DB"
```

### 5.3 写后失效缓存

1. admin 打开列表（预热缓存）  
2. `POST` 新留言  
3. 再刷新列表 → 应看到新留言（`Invalidate` + 从库读）

---

## 六、注意事项

|  topic | 说明 |
|--------|------|
| **复制延迟** | 写主后立刻读从，极少数情况下从库略慢；列表已配合 Redis 失效，一般可接受 |
| **从库只读** | `read_only=ON`；切勿对从库执行 `Migrate` 或业务写入 |
| **直改主库 SQL** | 会同步到从库，但 Redis 可能仍旧（与是否主从无关） |
| **从库故障** | 可临时去掉 `DefaultRead` 或改为 `mysql-master`，读降级到主库 |

---

## 七、文件清单

| 路径 | 作用 |
|------|------|
| `docker-compose.replica.yml` | 主从 + Redis + 双 API |
| `deploy/mysql/master.cnf` | 主库 binlog/GTID |
| `deploy/mysql/slave.cnf` | 从库只读 |
| `deploy/mysql/init-master/01-repl-user.sh` | 创建 `repl` 用户 |
| `deploy/mysql/setup-replication.sh` | 全量 dump + `START REPLICA` |

单库生产仍用 `docker-compose.prod.yml`；主从用 `docker-compose.replica.yml`。
