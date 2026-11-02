-- Insert only missing, non-null names so existing IDs are preserved and retries are safe.
INSERT INTO `services` (`name`)
SELECT DISTINCT events.`service_name` COLLATE utf8mb4_bin
FROM `container_events` AS events
LEFT JOIN `services` AS services
  ON services.`name` = events.`service_name` COLLATE utf8mb4_bin
WHERE events.`service_name` IS NOT NULL
  AND services.`id` IS NULL;

-- Match IDs to the recorded names, leaving events without a service unassigned.
UPDATE `container_events` AS events
LEFT JOIN `services` AS services
  ON services.`name` = events.`service_name` COLLATE utf8mb4_bin
SET events.`service_id` = services.`id`
WHERE NOT (events.`service_id` <=> services.`id`);
