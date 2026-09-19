namespace Maq.Compiler;

public class WorkItemException : Exception
{
    public WorkItemException(WorkItem workItem, Exception innerException)
        : base($"Work item failed: {innerException}", innerException)
    {
        WorkItem = workItem;
    }

    public WorkItem WorkItem { get; }
}
