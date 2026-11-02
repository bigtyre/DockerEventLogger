-- Insert only missing types so existing IDs are preserved and retries are safe.
INSERT INTO `event_types` (`name`)
SELECT DISTINCT events.`type` COLLATE utf8mb4_bin
FROM `container_events` AS events
LEFT JOIN `event_types` AS types
  ON types.`name` = events.`type` COLLATE utf8mb4_bin
WHERE types.`id` IS NULL;

-- Fill missing IDs and repair any IDs that do not match the recorded type.
UPDATE `container_events` AS events
INNER JOIN `event_types` AS types
  ON types.`name` = events.`type` COLLATE utf8mb4_bin
SET events.`type_id` = types.`id`
WHERE events.`type_id` IS NULL
  OR events.`type_id` <> types.`id`;
