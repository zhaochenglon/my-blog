-- 修复：迁移记录已存在但 messages 表未创建（空迁移导致）
-- 用法: docker exec -i blog-mysql mysql -uroot -pblog_dev_password blog_db < blog-api/scripts/fix-messages-table.sql

CREATE TABLE IF NOT EXISTS `messages` (
    `Id` bigint NOT NULL AUTO_INCREMENT,
    `Name` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
    `Email` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
    `Content` varchar(2000) CHARACTER SET utf8mb4 NOT NULL,
    `CreatedAt` datetime(6) NOT NULL,
    `IsRead` tinyint(1) NOT NULL,
    CONSTRAINT `PK_messages` PRIMARY KEY (`Id`)
) CHARACTER SET=utf8mb4;

-- MySQL 8.0 不支持 CREATE INDEX IF NOT EXISTS，索引可选
-- CREATE INDEX `IX_messages_CreatedAt` ON `messages` (`CreatedAt`);
-- CREATE INDEX `IX_messages_Email` ON `messages` (`Email`);
