#!/bin/bash
set -euo pipefail

repl_password="${MYSQL_REPLICATION_PASSWORD:-repl_dev_password}"

mysql -uroot -p"${MYSQL_ROOT_PASSWORD}" <<EOSQL
CREATE USER IF NOT EXISTS 'repl'@'%' IDENTIFIED WITH mysql_native_password BY '${repl_password}';
ALTER USER 'repl'@'%' IDENTIFIED WITH mysql_native_password BY '${repl_password}';
GRANT REPLICATION SLAVE ON *.* TO 'repl'@'%';
FLUSH PRIVILEGES;
EOSQL
