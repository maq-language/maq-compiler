namespace Maq.Compiler;

/// <summary>
/// A delegate representing the action taken when a work item is scheduled.
/// </summary>
public delegate Task WorkItemAction(IPublisher publisher);
