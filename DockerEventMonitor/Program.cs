using BigTyre.Docker.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.UserSecrets;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using System.Diagnostics;


var appSettings = GetAppSettings(args);

var mysqlConnectionString = appSettings.MySqlConnectionString;// ?? throw new Exception("MySQL Connection string not configured.");

var startInfo = new ProcessStartInfo
{
    FileName = "docker",
    Arguments = "events --format \"{{json .}}\" --filter 'type=container'",
    RedirectStandardOutput = true,
    UseShellExecute = false,
    CreateNoWindow = true
};

var process = new Process { StartInfo = startInfo };
process.Start();

while (!process.StandardOutput.EndOfStream)
{
    var line = process.StandardOutput.ReadLine();
    if (line is null)
        continue;

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

    var timeString = time.ToUniversalTime().DateTime.ToString("yyyy-MM-dd HH:mm:ss.fff");

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
    eventInsertCommand.CommandText = "INSERT INTO `container_events` (`time`, `type`, `container_name`, `service_name`, `image_id`) VALUES (@time, @type, @containerName, @serviceName, @imageId);";
    eventInsertCommand.Parameters.AddWithValue("time", timeString);
    eventInsertCommand.Parameters.AddWithValue("type", action);
    eventInsertCommand.Parameters.AddWithValue("containerName", containerName);
    eventInsertCommand.Parameters.AddWithValue("serviceName", serviceName);
    eventInsertCommand.Parameters.AddWithValue("imageId", imageId);

    eventInsertCommand.ExecuteNonQuery();

    conn.Close();
}

AppSettings GetAppSettings(string[] args)
{
    var config = GetAppConfiguration(args);
    var settings = new AppSettings();
    config.Bind(settings);
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