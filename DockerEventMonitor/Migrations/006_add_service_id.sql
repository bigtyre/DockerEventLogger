-- Check first so this migration can be retried if the DDL succeeded before the version was saved.
SET @service_id_sql = IF(
  EXISTS (
    SELECT 1 FROM information_schema.columns
    WHERE table_schema = DATABASE()
      AND table_name = 'container_events'
      AND column_name = 'service_id'
  ),
  'SELECT 1',
  'ALTER TABLE `container_events` ADD COLUMN `service_id` smallint unsigned DEFAULT NULL'
);

PREPARE service_id_statement FROM @service_id_sql;
EXECUTE service_id_statement;
DEALLOCATE PREPARE service_id_statement;
