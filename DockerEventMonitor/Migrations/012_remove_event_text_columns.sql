-- Remove legacy indexes and columns together, skipping anything already removed on a retry.
SET @remove_text_changes = CONCAT_WS(', ',
  IF(EXISTS (
    SELECT 1 FROM information_schema.statistics
    WHERE table_schema = DATABASE()
      AND table_name = 'container_events'
      AND index_name = 'container_name'
  ), 'DROP INDEX `container_name`', NULL),
  IF(EXISTS (
    SELECT 1 FROM information_schema.statistics
    WHERE table_schema = DATABASE()
      AND table_name = 'container_events'
      AND index_name = 'service_name'
  ), 'DROP INDEX `service_name`', NULL),
  IF(EXISTS (
    SELECT 1 FROM information_schema.statistics
    WHERE table_schema = DATABASE()
      AND table_name = 'container_events'
      AND index_name = 'type'
  ), 'DROP INDEX `type`', NULL),
  IF(EXISTS (
    SELECT 1 FROM information_schema.columns
    WHERE table_schema = DATABASE()
      AND table_name = 'container_events'
      AND column_name = 'container_name'
  ), 'DROP COLUMN `container_name`', NULL),
  IF(EXISTS (
    SELECT 1 FROM information_schema.columns
    WHERE table_schema = DATABASE()
      AND table_name = 'container_events'
      AND column_name = 'service_name'
  ), 'DROP COLUMN `service_name`', NULL),
  IF(EXISTS (
    SELECT 1 FROM information_schema.columns
    WHERE table_schema = DATABASE()
      AND table_name = 'container_events'
      AND column_name = 'type'
  ), 'DROP COLUMN `type`', NULL)
);

SET @remove_text_sql = IF(
  @remove_text_changes = '',
  'SELECT 1',
  CONCAT('ALTER TABLE `container_events` ', @remove_text_changes)
);

PREPARE remove_text_statement FROM @remove_text_sql;
EXECUTE remove_text_statement;
DEALLOCATE PREPARE remove_text_statement;
