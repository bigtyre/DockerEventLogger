-- Skip an existing view so a retry does not require DROP permissions.
SET @readable_events_sql = IF(EXISTS (
  SELECT 1 FROM information_schema.tables
  WHERE table_schema = DATABASE()
    AND table_name = 'container_events_readable'
    AND table_type = 'VIEW'
), 'SELECT 1',
  'CREATE SQL SECURITY INVOKER VIEW `container_events_readable` AS
   SELECT events.`id`,
          events.`time`,
          events.`container_id`,
          containers.`name` AS `container_name`,
          events.`service_id`,
          services.`name` AS `service_name`,
          events.`type_id`,
          types.`name` AS `type`,
          events.`image_id`,
          images.`name` AS `image_name`
   FROM `container_events` AS events
   LEFT JOIN `containers` AS containers ON containers.`id` = events.`container_id`
   LEFT JOIN `services` AS services ON services.`id` = events.`service_id`
   LEFT JOIN `event_types` AS types ON types.`id` = events.`type_id`
   LEFT JOIN `container_images` AS images ON images.`id` = events.`image_id`'
);

PREPARE readable_events_statement FROM @readable_events_sql;
EXECUTE readable_events_statement;
DEALLOCATE PREPARE readable_events_statement;
