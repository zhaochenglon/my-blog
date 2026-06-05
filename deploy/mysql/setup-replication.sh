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

echo "==> 从主库导出 blog_db 到从库..."
docker exec "${MASTER_CONTAINER}" mysqldump -uroot -p"${ROOT_PASSWORD}" \
  --single-transaction --set-gtid-purged=ON --databases blog_db \
  | docker exec -i "${SLAVE_CONTAINER}" mysql -uroot -p"${ROOT_PASSWORD}"

echo "==> 配置从库复制源并启动..."
docker exec -i "${SLAVE_CONTAINER}" mysql -uroot -p"${ROOT_PASSWORD}" <<EOSQL
STOP REPLICA;
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
