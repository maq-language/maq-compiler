namespace Maq.Compiler;

/// <summary>
/// Status of executing the compiler. Indicates completed (all work items executed), stalled (some work items
/// were not able to run due to unsatisfied dependencies), or faulted (internal exception thrown during the
/// execution of a work item).
/// </summary>
public enum CompileStatusCode
{
    Completed,
    Stalled,
    Faulted
}
