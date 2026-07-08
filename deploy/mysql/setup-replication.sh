#!/usr/bin/env bash
# 在主库已有数据、从库容器已启动后执行一次（GTID + 全库 dump）。
# 用法（在项目根目录）：
#   export MYSQL_ROOT_PASSWORD='你的密码'
#   export MYSQL_REPLICATION_PASSWORD='repl密码'   # 与 init-master 中一致
#   bash deploy/mysql/setup-replication.sh

set -euo pipefail

MASTER_CONTAINER="${MASTER_CONTAINER:-blog-mysql-master}"
SLAVE_CONTAINER="${SLAVE_CONTAINER:-blog-mysql-slave}"
ROOT_PASSWORD="${MYSQL_ROOT_PASSWORD:?请设置 MYSQL_ROOT_PASSWORD}"
ROOT_PASSWORD="${ROOT_PASSWORD%"${ROOT_PASSWORD##*[![:space:]]}"}"
REPL_PASSWORD="${MYSQL_REPLICATION_PASSWORD:-repl_dev_password}"
REPL_PASSWORD="${REPL_PASSWORD%"${REPL_PASSWORD##*[![:space:]]}"}"

if [ "${#REPL_PASSWORD}" -gt 32 ]; then
  echo "ERROR: MYSQL_REPLICATION_PASSWORD 长度 ${#REPL_PASSWORD} 超过 MySQL 复制用户上限 32 字符。"
  echo "       请改用 ≤32 位的密码（UUID 请去掉连字符，或另设随机串）。"
  exit 1
fi

echo "==> 重置从库复制状态与 GTID（支持重复执行，避免 GTID_PURGED 与 GTID_EXECUTED 冲突）..."
docker exec -i "${SLAVE_CONTAINER}" mysql -uroot -p"${ROOT_PASSWORD}" <<'EOSQL'
STOP REPLICA;
RESET REPLICA ALL;
RESET MASTER;
SET GLOBAL read_only = OFF;
SET GLOBAL super_read_only = OFF;
EOSQL

echo "==> 从主库导出 blog_db 到从库..."
docker exec "${MASTER_CONTAINER}" mysqldump -uroot -p"${ROOT_PASSWORD}" \
  --single-transaction --set-gtid-purged=ON --databases blog_db \
  | docker exec -i "${SLAVE_CONTAINER}" mysql -uroot -p"${ROOT_PASSWORD}"

echo "==> 确保主库 repl 用户可用于非 SSL 复制（避免 caching_sha2 要求安全连接）..."
docker exec -i "${MASTER_CONTAINER}" mysql -uroot -p"${ROOT_PASSWORD}" <<EOSQL
ALTER USER 'repl'@'%' IDENTIFIED WITH mysql_native_password BY '${REPL_PASSWORD}';
GRANT REPLICATION SLAVE ON *.* TO 'repl'@'%';
FLUSH PRIVILEGES;
EOSQL

echo "==> 配置从库复制源并启动..."
docker exec -i "${SLAVE_CONTAINER}" mysql -uroot -p"${ROOT_PASSWORD}" <<EOSQL
STOP REPLICA;
CHANGE REPLICATION FILTER REPLICATE_DO_DB = (blog_db);
CHANGE REPLICATION SOURCE TO
  SOURCE_HOST='mysql-master',
  SOURCE_USER='repl',
  SOURCE_PASSWORD='${REPL_PASSWORD}',
  SOURCE_AUTO_POSITION=1;
START REPLICA;
SET GLOBAL read_only = ON;
SET GLOBAL super_read_only = ON;
EOSQL

echo "==> 复制状态："
docker exec "${SLAVE_CONTAINER}" mysql -uroot -p"${ROOT_PASSWORD}" -e \
  "SHOW REPLICA STATUS\G" | grep -E "Replica_IO_Running|Replica_SQL_Running|Seconds_Behind_Source|Last_Error" || true

echo "完成。请确认 Replica_IO_Running 与 Replica_SQL_Running 均为 Yes。"
