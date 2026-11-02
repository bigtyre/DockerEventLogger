using BigTyre.Docker.Configuration;
using BigTyre.Docker;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using System.Diagnostics;


AppSettings appSettings;

try
{
    appSettings = GetAppSettings(args);
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Configuration error: {ex.Message}");
    Environment.ExitCode = 1;
    return;
}

var mysqlConnectionString = appSettings.MySqlConnectionString;

try
{
    DatabaseMigrator.Migrate(mysqlConnectionString);
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Database setup failed: {ex.Message}");
    Environment.ExitCode = 1;
    return;
}

var startInfo = new ProcessStartInfo
{
    FileName = "docker",
    Arguments = "events --format \"{{json .}}\" --filter 'type=container'",
    RedirectStandardOutput = true,
    UseShellExecute = false,
    CreateNoWindow = true
};

using var process = new Process { StartInfo = startInfo };
process.Start();

using var retentionCancellation = new CancellationTokenSource();
var retentionTask = EventRetention.RunAsync(mysqlConnectionString!, appSettings.RetentionDays, retentionCancellation.Token);

try
{
    while (true)
    {
        var line = await process.StandardOutput.ReadLineAsync();
        if (line is null)
            break;

        try
        {
            var dockerEvent = JsonConvert.DeserializeObject<DockerEvent>(line);
            if (dockerEvent is null)
                continue;

            var action = dockerEvent.Action;
            if (action is null)
                continue;

            if (action.StartsWith("exec_"))
                continue;


            var type = dockerEvent.Type;
            if (type is null)
                continue;

            if (type != "container")
                continue;

            var imageName = dockerEvent.From;
            var timestamp = dockerEvent.Time;
            var time = DateTimeOffset.FromUnixTimeSeconds(timestamp);
            var actor = dockerEvent.Actor;

            string? serviceName = null;
            string? containerName = null;
            if (actor is not null)
            {
                var attributes = actor.Attributes;
                attributes.TryGetValue("com.docker.swarm.service.name", out serviceName);
                attributes.TryGetValue("name", out containerName);
            }

            Console.WriteLine($"{time:O}, Action: {action}, Service: {serviceName ?? "NULL"}, Container: {containerName ?? "NULL"}");

            if (containerName is null)
            {
                Console.WriteLine("Container name was null. Event will not be written to the database.");
                continue;
            }

            HandleContainerEvent(time, action, containerName, serviceName, imageName);
            //Console.WriteLine($"{JsonConvert.SerializeObject(dockerEvent, Formatting.Indented)}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error while processing docker event: {ex.Message}");
        }
    }
}
finally
{
    retentionCancellation.Cancel();
    await retentionTask;
}

Console.WriteLine("Docker events process ended. Application closing...");


/// <summary>Stores an event using lookup IDs for its container, service, and type.</summary>
void HandleContainerEvent(
    DateTimeOffset time, 
    string action, 
    string containerName, 
    string? serviceName,
    string? imageName
)
{
    if (mysqlConnectionString is null)
        return;

    using var conn = new MySqlConnection(mysqlConnectionString);

    conn.Open();

    var containerId = GetContainerId(conn, containerName);
    var typeId = GetEventTypeId(conn, action);
    var timeString = time.ToUniversalTime().DateTime.ToString("yyyy-MM-dd HH:mm:ss.fff");

    ushort? serviceId = null;

    if (serviceName is not null)
    {
        serviceId = GetServiceId(conn, serviceName);
    }

    int? imageId = null;

    if (imageName is not null)
    {
        using var imageLookupCommand = conn.CreateCommand();
        imageLookupCommand.CommandText = "SELECT `id` FROM `container_images` WHERE `name` = @imageName LIMIT 1;";
        imageLookupCommand.Parameters.AddWithValue("imageName", imageName);

        var id = imageLookupCommand.ExecuteScalar();
        if (id is not null)
        {
            imageId = Convert.ToInt32(id);
        }
        else
        {
            using var imageInsertCommand = conn.CreateCommand();
            imageInsertCommand.CommandText = "INSERT INTO `container_images` (`name`) VALUES (@imageName);";
            imageInsertCommand.Parameters.AddWithValue("imageName", imageName);

            imageInsertCommand.ExecuteNonQuery();

            using var insertIdCommand = conn.CreateCommand();
            insertIdCommand.CommandText = "SELECT LAST_INSERT_ID();";

            id = imageLookupCommand.ExecuteScalar();
            if (id is not null)
            {
                imageId = Convert.ToInt32(id);
            }
        }
    }


    using var eventInsertCommand = conn.CreateCommand();
    eventInsertCommand.CommandText = "INSERT INTO `container_events` (`time`, `type_id`, `container_id`, `service_id`, `image_id`) VALUES (@time, @typeId, @containerId, @serviceId, @imageId);";
    eventInsertCommand.Parameters.AddWithValue("time", timeString);
    eventInsertCommand.Parameters.AddWithValue("typeId", typeId);
    eventInsertCommand.Parameters.AddWithValue("containerId", containerId);
    eventInsertCommand.Parameters.AddWithValue("serviceId", serviceId);
    eventInsertCommand.Parameters.AddWithValue("imageId", imageId);

    eventInsertCommand.ExecuteNonQuery();

    conn.Close();
}

/// <summary>Finds or creates a container, safely handling concurrent inserts of the same name.</summary>
uint GetContainerId(MySqlConnection conn, string containerName)
{
    using var lookupCommand = conn.CreateCommand();
    lookupCommand.CommandText = "SELECT `id` FROM `containers` WHERE `name` = @containerName LIMIT 1;";
    lookupCommand.Parameters.AddWithValue("containerName", containerName);

    var id = lookupCommand.ExecuteScalar();

    if (id is null)
    {
        using var insertCommand = conn.CreateCommand();
        insertCommand.CommandText = """
            INSERT INTO `containers` (`name`) VALUES (@containerName)
            ON DUPLICATE KEY UPDATE `id` = LAST_INSERT_ID(`id`);
            """;
        insertCommand.Parameters.AddWithValue("containerName", containerName);
        insertCommand.ExecuteNonQuery();

        id = insertCommand.LastInsertedId;
    }

    var containerId = Convert.ToUInt32(id);
    return containerId;
}

/// <summary>Finds or creates a service, safely handling concurrent inserts of the same name.</summary>
ushort GetServiceId(MySqlConnection conn, string serviceName)
{
    using var lookupCommand = conn.CreateCommand();
    lookupCommand.CommandText = "SELECT `id` FROM `services` WHERE `name` = @serviceName LIMIT 1;";
    lookupCommand.Parameters.AddWithValue("serviceName", serviceName);

    var id = lookupCommand.ExecuteScalar();

    if (id is null)
    {
        using var insertCommand = conn.CreateCommand();
        insertCommand.CommandText = """
            INSERT INTO `services` (`name`) VALUES (@serviceName)
            ON DUPLICATE KEY UPDATE `id` = LAST_INSERT_ID(`id`);
            """;
        insertCommand.Parameters.AddWithValue("serviceName", serviceName);
        insertCommand.ExecuteNonQuery();

        id = insertCommand.LastInsertedId;
    }

    var serviceId = Convert.ToUInt16(id);
    return serviceId;
}

/// <summary>Finds or creates an event type, safely handling concurrent inserts of the same name.</summary>
ushort GetEventTypeId(MySqlConnection conn, string typeName)
{
    using var lookupCommand = conn.CreateCommand();
    lookupCommand.CommandText = "SELECT `id` FROM `event_types` WHERE `name` = @typeName LIMIT 1;";
    lookupCommand.Parameters.AddWithValue("typeName", typeName);

    var id = lookupCommand.ExecuteScalar();

    if (id is null)
    {
        using var insertCommand = conn.CreateCommand();
        insertCommand.CommandText = """
            INSERT INTO `event_types` (`name`) VALUES (@typeName)
            ON DUPLICATE KEY UPDATE `id` = LAST_INSERT_ID(`id`);
            """;
        insertCommand.Parameters.AddWithValue("typeName", typeName);
        insertCommand.ExecuteNonQuery();

        id = insertCommand.LastInsertedId;
    }

    var typeId = Convert.ToUInt16(id);
    return typeId;
}

/// <summary>Binds configuration and rejects an invalid retention period before startup.</summary>
AppSettings GetAppSettings(string[] args)
{
    var config = GetAppConfiguration(args);
    var settings = new AppSettings();
    config.Bind(settings);

    if (settings.RetentionDays < 1)
    {
        throw new InvalidOperationException("RetentionDays must be at least 1.");
    }

    return settings;
}

IConfiguration GetAppConfiguration(string[] args)
{
    var configBuilder = new ConfigurationBuilder();
    configBuilder.AddEnvironmentVariables();
    configBuilder.AddKeyPerFile("/run/secrets", optional: true);
    var config = configBuilder.Build();

    /*
    var configValues = config.AsEnumerable();
    var numConfigValues = configValues.Count();
    Console.WriteLine($"# Values configured: {numConfigValues}");

    foreach (var configItem in configValues)
    {
        Console.WriteLine($"{configItem.Key} = {configItem.Value}");
    }*/

    return config;
}
