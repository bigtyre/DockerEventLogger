-- Preserve distinct container names, including names that differ only in case.
ALTER TABLE `containers`
  MODIFY COLUMN `name` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_bin NOT NULL;

-- Insert only missing names so existing IDs are preserved and retries are safe.
INSERT INTO `containers` (`name`)
SELECT DISTINCT events.`container_name` COLLATE utf8mb4_bin
FROM `container_events` AS events
LEFT JOIN `containers` AS containers
  ON containers.`name` = events.`container_name` COLLATE utf8mb4_bin
WHERE containers.`id` IS NULL;

-- Fill missing IDs and repair any IDs that do not match the recorded name.
UPDATE `container_events` AS events
INNER JOIN `containers` AS containers
  ON containers.`name` = events.`container_name` COLLATE utf8mb4_bin
SET events.`container_id` = containers.`id`
WHERE events.`container_id` IS NULL
  OR events.`container_id` <> containers.`id`;
