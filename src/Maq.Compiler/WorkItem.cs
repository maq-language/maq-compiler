namespace Maq.Compiler;

public class WorkItem
{
    public WorkItem(string name, IEnumerable<Dependency> dependencies, WorkItemAction action)
    {
        Name = name;
        Dependencies = dependencies.ToArray();
        Run = action;
    }

    public string Name { get; }

    public IReadOnlyCollection<Dependency> Dependencies { get; }

    public WorkItemAction Run { get; }

    public bool IsReady => Dependencies.All(x => x.IsSatisfied);

    public override string ToString() => Name;
}
