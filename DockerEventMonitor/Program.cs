using Newtonsoft.Json;
using System;
using System.Diagnostics;

class DockerEventsWatcher
{
    static void Main()
    {
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

                Console.WriteLine($"{time:O}, Action: {action}, Image: {imageName},  Service: {serviceName ?? "NULL"}, Container: {containerName ?? "NULL"}");
                Console.WriteLine($"{JsonConvert.SerializeObject(dockerEvent, Formatting.Indented)}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while processing docker event: {ex.Message}");
            }
        }
    }
}
