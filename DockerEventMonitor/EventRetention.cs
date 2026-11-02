using MySql.Data.MySqlClient;

namespace BigTyre.Docker;

internal static class EventRetention
{
    private const int BatchSize = 5000;

    /// <summary>Runs cleanup at startup and hourly without blocking event monitoring.</summary>
    public static async Task RunAsync(string connectionString, int retentionDays, CancellationToken cancellationToken)
    {
        if (retentionDays < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(retentionDays), retentionDays, "Retention period must be at least 1 day.");
        }

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var cutoffUtc = DateTime.UtcNow.AddDays(-retentionDays);
                    var deletedCount = await DeleteExpiredEventsAsync(connectionString, cutoffUtc, cancellationToken);

                    if (deletedCount > 0)
                        Console.WriteLine($"Retention cleanup deleted {deletedCount} events older than {cutoffUtc:O}.");
                }
                catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
                {
                    Console.Error.WriteLine($"Retention cleanup failed: {ex.Message}");
                }

                await Task.Delay(TimeSpan.FromHours(1), cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Normal shutdown can interrupt a query or the delay between cleanup runs.
        }
    }

    /// <summary>Deletes expired events in small batches using one connection and command for the run.</summary>
    internal static async Task<long> DeleteExpiredEventsAsync(string connectionString, DateTime cutoffUtc, CancellationToken cancellationToken)
    {
        using var conn = new MySqlConnection(connectionString);
        await conn.OpenAsync(cancellationToken);

        using var command = conn.CreateCommand();
        command.CommandText = """
            DELETE FROM `container_events`
            WHERE `time` < @cutoff
            ORDER BY `time`
            LIMIT 5000;
            """;
        command.Parameters.AddWithValue("cutoff", cutoffUtc);

        long deletedCount = 0;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Each batch commits separately; avoid a large transaction over the entire backlog.
            var batchCount = await command.ExecuteNonQueryAsync(cancellationToken);
            deletedCount += batchCount;

            if (batchCount < BatchSize)
                break;

            await Task.Delay(TimeSpan.FromMilliseconds(250), cancellationToken);
        }

        return deletedCount;
    }
}
