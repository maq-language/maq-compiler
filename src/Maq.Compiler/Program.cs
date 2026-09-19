using System.Diagnostics;
using Maq.Compiler;

var symbols = new SymbolTable();
var generated = symbols.GetOrCreate("Generated");
var schedule = new WorkSchedule();

schedule.Submit(
    new WorkItem(
        "Parse",
        [],
        work =>
        {
            work.Submit(
                new WorkItem(
                    "Typecheck Main",
                    [new Dependency(generated, SymbolFacts.SignatureKnown)],
                    _ => Task.CompletedTask));

            work.Submit(
                new WorkItem(
                    "Expand Macro",
                    [],
                    work =>
                    {
                        work.Publish(
                            generated,
                            SymbolFacts.Declared |
                            SymbolFacts.SignatureKnown);

                        return Task.CompletedTask;
                    }));

            return Task.CompletedTask;
        }));

await schedule.Completion;
