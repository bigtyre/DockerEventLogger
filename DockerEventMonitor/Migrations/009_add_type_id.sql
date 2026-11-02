-- Check first so this migration can be retried if the DDL succeeded before the version was saved.
SET @type_id_sql = IF(
  EXISTS (
    SELECT 1 FROM information_schema.columns
    WHERE table_schema = DATABASE()
      AND table_name = 'container_events'
      AND column_name = 'type_id'
  ),
  'SELECT 1',
  'ALTER TABLE `container_events` ADD COLUMN `type_id` smallint unsigned DEFAULT NULL'
);

PREPARE type_id_statement FROM @type_id_sql;
EXECUTE type_id_statement;
DEALLOCATE PREPARE type_id_statement;
