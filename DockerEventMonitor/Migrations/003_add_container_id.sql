-- Check first so this migration can be retried if the DDL succeeded before the version was saved.
SET @container_id_sql = IF(
  EXISTS (
    SELECT 1 FROM information_schema.columns
    WHERE table_schema = DATABASE()
      AND table_name = 'container_events'
      AND column_name = 'container_id'
  ),
  'SELECT 1',
  'ALTER TABLE `container_events` ADD COLUMN `container_id` int unsigned DEFAULT NULL'
);

PREPARE container_id_statement FROM @container_id_sql;
EXECUTE container_id_statement;
DEALLOCATE PREPARE container_id_statement;
