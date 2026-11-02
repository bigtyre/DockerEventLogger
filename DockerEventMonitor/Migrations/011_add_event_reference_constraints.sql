-- Build one ALTER statement, retaining existing indexes and allowing retries after partial setup.
SET @reference_changes = CONCAT_WS(', ',
  IF(EXISTS (
    SELECT 1 FROM information_schema.statistics
    WHERE table_schema = DATABASE()
      AND table_name = 'container_events'
      AND column_name = 'container_id'
      AND seq_in_index = 1
  ), NULL, 'ADD INDEX `container_id` (`container_id`)'),
  IF(EXISTS (
    SELECT 1 FROM information_schema.statistics
    WHERE table_schema = DATABASE()
      AND table_name = 'container_events'
      AND column_name = 'service_id'
      AND seq_in_index = 1
  ), NULL, 'ADD INDEX `service_id` (`service_id`)'),
  IF(EXISTS (
    SELECT 1 FROM information_schema.statistics
    WHERE table_schema = DATABASE()
      AND table_name = 'container_events'
      AND column_name = 'type_id'
      AND seq_in_index = 1
  ), NULL, 'ADD INDEX `type_id` (`type_id`)'),
  IF(EXISTS (
    SELECT 1 FROM information_schema.referential_constraints
    WHERE constraint_schema = DATABASE()
      AND table_name = 'container_events'
      AND constraint_name = 'container_events_container_fk'
  ), NULL, 'ADD CONSTRAINT `container_events_container_fk` FOREIGN KEY (`container_id`) REFERENCES `containers` (`id`) ON DELETE RESTRICT ON UPDATE CASCADE'),
  IF(EXISTS (
    SELECT 1 FROM information_schema.referential_constraints
    WHERE constraint_schema = DATABASE()
      AND table_name = 'container_events'
      AND constraint_name = 'container_events_service_fk'
  ), NULL, 'ADD CONSTRAINT `container_events_service_fk` FOREIGN KEY (`service_id`) REFERENCES `services` (`id`) ON DELETE RESTRICT ON UPDATE CASCADE'),
  IF(EXISTS (
    SELECT 1 FROM information_schema.referential_constraints
    WHERE constraint_schema = DATABASE()
      AND table_name = 'container_events'
      AND constraint_name = 'container_events_type_fk'
  ), NULL, 'ADD CONSTRAINT `container_events_type_fk` FOREIGN KEY (`type_id`) REFERENCES `event_types` (`id`) ON DELETE RESTRICT ON UPDATE CASCADE')
);

SET @reference_sql = IF(
  @reference_changes = '',
  'SELECT 1',
  CONCAT('ALTER TABLE `container_events` ', @reference_changes)
);

PREPARE reference_statement FROM @reference_sql;
EXECUTE reference_statement;
DEALLOCATE PREPARE reference_statement;
