# Docker Event Logger

This app logs docker events to a MySQL database.

Before running the app, copy `example.env` to `.env` in the solution root:

```powershell
Copy-Item example.env .env
```

Edit `.env` and replace the placeholders in `MySqlConnectionString` with your MySQL host, database name, username, and password.

Then start the app using Docker Compose:

```sh
docker compose up --build -d
```

The included Compose configuration loads `.env` into the container and mounts `/var/run/docker.sock` read-only so the app can monitor Docker events.

## Automatic database setup

The MySQL server must already be running and reachable. Before monitoring Docker events, the app creates the database named in `MySqlConnectionString` if it does not exist, then creates or updates its tables automatically. No manual schema script is required.

The app tracks the schema version in a single row in `schema_version` and applies embedded SQL migrations in order. Existing installations using the original schema are adopted without deleting data; existing tables are assumed to match that schema. A MySQL advisory lock prevents multiple app instances from running migrations simultaneously.

If database setup fails, or the database version is newer than the app supports, the app logs an error and exits with a nonzero exit code before monitoring events.

Events store `container_id`, `service_id`, and `type_id`; join `containers`, `services`, and `event_types` to retrieve their names. Migration 12 removes the original text columns and their indexes after the earlier backfill migrations. Stop older app instances before upgrading, since they still write those removed columns.

## Querying events

The `container_events_readable` view includes event IDs, timestamps, lookup IDs, and readable `container_name`, `service_name`, `type`, and `image_name` columns. Events without a service or image are included with null names.

```sql
SELECT * FROM container_events_readable ORDER BY time DESC LIMIT 100;
```

## Retention setting

`RetentionDays` defaults to `90` and can be overridden in `.env`, for example `RetentionDays=30`. It must be greater than zero.

Cleanup runs in the background at startup and hourly, deleting events older than the configured number of days using UTC timestamps. Each batch deletes at most 5,000 events, with a 250 ms pause between full batches. A cleanup run reuses one connection and command, then releases them before waiting for the next run. Lookup tables are retained.

Cleanup failures are logged and retried on the next hourly run while event monitoring continues. The database account needs `DELETE` permission on `container_events`.

## MySQL permissions

The account in `MySqlConnectionString` needs `CREATE`, `CREATE VIEW`, `ALTER`, `SELECT`, `INSERT`, `UPDATE`, `DELETE`, and `REFERENCES` permissions on the configured database. `CREATE` covers creating the database and tables; `CREATE VIEW` allows the readable events view; `ALTER` allows migrations to change columns, indexes, and constraints; `REFERENCES` permits foreign keys from events to the lookup tables; `DELETE` allows retention cleanup. A MySQL administrator can grant these permissions before the database exists, for example:

```sql
GRANT CREATE, CREATE VIEW, ALTER, SELECT, INSERT, UPDATE, DELETE, REFERENCES
ON `docker_events`.* TO 'docker_event_logger'@'%';
```

Replace the database name and account with your own; the MySQL account must already exist. Future migrations may require additional permissions, such as `INDEX` or `DROP`, depending on their SQL.

## Adding migrations

Add the next numbered SQL file to `DockerEventMonitor/Migrations` and append its embedded resource name (for example, `Migrations.002_add_column.sql`) to the list in `DatabaseMigrator.cs`. Files in this directory are embedded automatically, so migrations ship with the app.

Migrations are forward-only: never reorder or edit an applied migration. The version advances only after the entire script succeeds. MySQL DDL commits implicitly, so a failed migration can leave partial changes behind. Write scripts that are safe to retry, or repair the partial changes before restarting. Keep a database backup before applying schema changes.
