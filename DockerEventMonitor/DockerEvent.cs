
/// <summary>
/// Represents a Docker event.
/// </summary>
public class DockerEvent
{
    /// <summary>
    /// The status of the event.
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// The unique ID associated with the event.
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// The source from which the event originates.
    /// </summary>
    public string? From { get; set; }

    /// <summary>
    /// Represents the type of the Docker event.<br />
    /// Possible values include:<br />
    /// - "container": Events related to Docker containers.<br />
    /// - "image": Events related to Docker images.<br />
    /// Other relevant types specific to different Docker event categories.
    /// </summary>

    public string? Type { get; set; }

    /// <summary>
    /// Represents the action that triggered the Docker event. Possible values vary based on the specific event type and context, but examples include:<br />
    /// - "create": Signifies the creation of a Docker resource (e.g., a container).<br />
    /// - "destroy": Indicates the removal or destruction of a Docker resource.<br />
    /// - "start": Denotes the starting of a previously created Docker resource (e.g., container).<br />
    /// - "stop": Represents the stopping of a running Docker resource.<br />
    /// - "restart": Signifies the restarting of a Docker resource.<br />
    /// - "exec_start": Indicates the execution of a command within a Docker container.<br />
    /// - Other values specific to different Docker actions and events.
    /// </summary>
    public string? Action { get; set; }

    /// <summary>
    /// The actor associated with the event.
    /// </summary>
    public Actor? Actor { get; set; }

    /// <summary>
    /// Represents the scope or context in which the Docker event occurred. Possible values include:<br />
    /// - "local": The event took place within the local Docker environment on a single host.<br />
    /// - "swarm": Indicates that the event is related to a Docker Swarm cluster.<br />
    /// - "node": Specifies that the event pertains to a specific node within a Docker Swarm cluster.<br />
    /// - "cluster": Denotes that the event is relevant to the entire Docker cluster or swarm.<br />
    /// - "plugin": Used for events related to Docker plugins.<br />
    /// - "system": Signifies events related to Docker system-level operations or configurations.<br />
    /// - "service": In a Docker Swarm context, events related to services may have this scope.<br />
    /// - "container": Indicates that the event is specific to an individual Docker container.
    /// </summary>
    public string? Scope { get; set; }

    /// <summary>
    /// The time when the event occurred in Unix timestamp format.
    /// </summary>
    public long Time { get; set; }

    /// <summary>
    /// The time when the event occurred with nanosecond precision.
    /// </summary>
    public long TimeNano { get; set; }
}
