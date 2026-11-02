using MySql.Data.MySqlClient;
using System.Security.Cryptography;
using System.Text;

namespace BigTyre.Docker
{
    internal static class DatabaseMigrator
    {
        // Position in this list determines the version. Append migrations; never edit applied SQL.
        private static readonly string[] Migrations =
        [
            "Migrations.001_initial.sql",
            "Migrations.002_create_containers.sql",
            "Migrations.003_add_container_id.sql",
            "Migrations.004_backfill_containers.sql",
            "Migrations.005_create_services.sql",
            "Migrations.006_add_service_id.sql",
            "Migrations.007_backfill_services.sql",
            "Migrations.008_create_event_types.sql",
            "Migrations.009_add_type_id.sql",
            "Migrations.010_backfill_event_types.sql",
            "Migrations.011_add_event_reference_constraints.sql",
            "Migrations.012_remove_event_text_columns.sql",
            "Migrations.013_create_readable_events_view.sql"
        ];

        /// <summary>Creates the database and applies pending migrations before event monitoring starts.</summary>
        public static void Migrate(string? connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException("MySqlConnectionString must be configured.");

            var builder = new MySqlConnectionStringBuilder(connectionString);
            var databaseName = builder.Database;

            if (string.IsNullOrWhiteSpace(databaseName))
                throw new InvalidOperationException("MySqlConnectionString must include a database name.");

            builder.Database = "";
            builder.AllowUserVariables = true;

            // Closing a dedicated, non-pooled connection releases the advisory lock, even on failure.
            builder.Pooling = false;

            using var conn = new MySqlConnection(builder.ConnectionString);
            conn.Open();

            AcquireMigrationLock(conn, databaseName);

            var escapedDatabaseName = databaseName.Replace("`", "``");
            ExecuteSql(conn, $"CREATE DATABASE IF NOT EXISTS `{escapedDatabaseName}`;");
            conn.ChangeDatabase(databaseName);

            EnsureVersionTable(conn);

            var version = GetVersion(conn);
            if (version < 0 || version > Migrations.Length)
                throw new InvalidOperationException($"Database schema version {version} is not supported by this application.");

            while (version < Migrations.Length)
            {
                var resourceName = Migrations[version];
                var nextVersion = version + 1;

                Console.WriteLine($"Applying database migration {nextVersion}: {resourceName}");
                ApplyMigration(conn, resourceName, nextVersion);

                version = nextVersion;
            }

            Console.WriteLine($"Database schema is at version {version}.");
        }

        /// <summary>Serializes startup migrations for the same database across application instances.</summary>
        private static void AcquireMigrationLock(MySqlConnection conn, string databaseName)
        {
            var lockKey = $"DockerEventMonitor:{databaseName.ToUpperInvariant()}";
            var lockBytes = Encoding.UTF8.GetBytes(lockKey);
            var lockHash = SHA256.HashData(lockBytes);
            var lockName = Convert.ToHexString(lockHash);

            using var command = conn.CreateCommand();
            command.CommandText = "SELECT GET_LOCK(@lockName, 30);";
            command.CommandTimeout = 35;
            command.Parameters.AddWithValue("lockName", lockName);

            var result = command.ExecuteScalar();
            if (result is null || result is DBNull || Convert.ToInt32(result) != 1)
                throw new InvalidOperationException("Could not acquire the database migration lock within 30 seconds.");
        }

        /// <summary>Initializes the single version row for a new or existing installation.</summary>
        private static void EnsureVersionTable(MySqlConnection conn)
        {
            ExecuteSql(conn, """
                CREATE TABLE IF NOT EXISTS `schema_version` (
                    `id` int NOT NULL,
                    `version` int NOT NULL,
                    PRIMARY KEY (`id`),
                    CONSTRAINT `schema_version_single_row` CHECK (`id` = 1)
                ) ENGINE=InnoDB;
                """);

            ExecuteSql(conn, """
                INSERT INTO `schema_version` (`id`, `version`)
                SELECT 1, 0 WHERE NOT EXISTS (SELECT 1 FROM `schema_version` WHERE `id` = 1);
                """);
        }

        /// <summary>Reads the last successfully applied migration version.</summary>
        private static int GetVersion(MySqlConnection conn)
        {
            using var command = conn.CreateCommand();
            command.CommandText = "SELECT `version` FROM `schema_version` WHERE `id` = 1;";

            var result = command.ExecuteScalar();
            if (result is null || result is DBNull)
                throw new InvalidOperationException("The database schema version row is missing.");

            var version = Convert.ToInt32(result);
            return version;
        }

        /// <summary>Executes embedded SQL and records its version only after it succeeds.</summary>
        private static void ApplyMigration(MySqlConnection conn, string resourceName, int version)
        {
            var assembly = typeof(DatabaseMigrator).Assembly;
            using var stream = assembly.GetManifestResourceStream(resourceName);

            if (stream is null)
                throw new InvalidOperationException($"Embedded migration {resourceName} was not found.");

            using var reader = new StreamReader(stream);
            var sql = reader.ReadToEnd();

            // MySQL DDL commits implicitly. Each migration must be safe to retry after partial failure.
            ExecuteSql(conn, sql);

            using var command = conn.CreateCommand();
            command.CommandText = "UPDATE `schema_version` SET `version` = @version WHERE `id` = 1;";
            command.Parameters.AddWithValue("version", version);
            command.ExecuteNonQuery();
        }

        /// <summary>Executes schema setup or migration SQL on the migration connection.</summary>
        private static void ExecuteSql(MySqlConnection conn, string sql)
        {
            using var command = conn.CreateCommand();
            command.CommandText = sql;

            // Schema changes and backfills on large event tables can exceed the default timeout.
            command.CommandTimeout = 0;
            command.ExecuteNonQuery();
        }
    }
}
