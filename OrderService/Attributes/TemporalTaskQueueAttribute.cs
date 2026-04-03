namespace OrderService.Attributes;

/// <summary>
/// Attribute to attach task queue name to the workflow or worker class of the Temporal.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property)]
public class TemporalTaskQueueAttribute : Attribute
{
    public TemporalTaskQueueAttribute(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentNullException("Task queue name of Temporal must be provided.");

        Name = name;
    }

    /// <summary>
    /// Gets or sets the task queue for the workflow or worker.
    /// </summary>
    public string Name { get; }
}