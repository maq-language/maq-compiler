namespace Maq.Compiler;

/// <summary>
/// A publisher enables broadcasting changes to facts of symbols and submitting new jobs.
/// </summary>
public interface IPublisher
{
    public void Publish(Symbol symbol, SymbolFacts facts);
    public void Submit(WorkItem workItem);
    public void Submit(string name, IEnumerable<Dependency> dependencies, WorkItemAction action);
}
