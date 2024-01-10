
/// <summary>
/// Represents an actor associated with a Docker event.
/// </summary>
public class Actor
{
    /// <summary>
    /// The ID of the actor.
    /// </summary>
    public string? ID { get; set; }

    /// <summary>
    /// The attributes of the actor as a dictionary.
    /// </summary>
    public Dictionary<string, string> Attributes { get; set; } = new();
}