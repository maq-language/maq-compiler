using Maq.Compiler;

namespace Maq.Compiler.Tests;

[TestClass]
public sealed class WorkScheduleTests
{
    [TestMethod]
    public async Task TestScheduleRunsUntilCanceled()
    {
        var workSchedule = new WorkSchedule();

        // NOTE(alex): If a job is never added to the schedule, the worker will wait for one to be added.
        await Assert.ThrowsAsync<TimeoutException>(() => workSchedule.Completion.WaitAsync(TimeSpan.FromTicks(10)));
        Assert.AreEqual(0, workSchedule.Completed);
    }

    [TestMethod]
    public async Task TestScheduleRunsUntilJobCompletes()
    {
        var workSchedule = new WorkSchedule();

        workSchedule.Submit("Parse", [], _ => Task.Delay(10));

        await workSchedule.Completion;
        Assert.AreEqual(1, workSchedule.Completed);
    }

    [TestMethod]
    public async Task TestScheduleRunsSpawnedJobs()
    {
        var workSchedule = new WorkSchedule();

        // NOTE(alex): Job-ception!
        workSchedule.Submit("Parse", [], static async work =>
        {
            await Task.Delay(10);
            work.Submit("Typecheck", [], static async work =>
            {
                await Task.Delay(10);
                work.Submit("Compile", [], _ => Task.Delay(10));
            });
        });

        await workSchedule.Completion;
        Assert.AreEqual(3, workSchedule.Completed);
    }

    [TestMethod]
    public async Task TestScheduleThrowsOnCircularDependency()
    {
        var symbolTable = new SymbolTable();
        var foo = symbolTable.GetOrCreate("Foo");

        var workSchedule = new WorkSchedule();

        // NOTE(alex): This is basically like saying "to parse Foo, we need Foo to have been parsed."
        workSchedule.Submit($"Parse {foo.Name}", [new Dependency(foo, SymbolFacts.SignatureKnown)], static _ => Task.Delay(10));

        await Assert.ThrowsAsync<CompilationStalledException>(() => workSchedule.Completion);
        Assert.AreEqual(0, workSchedule.Completed);
    }

    [TestMethod]
    public async Task TestScheduleThrowsOnMutuallyRecursiveJobs()
    {
        var symbolTable = new SymbolTable();
        var foo = symbolTable.GetOrCreate("Foo");
        var bar = symbolTable.GetOrCreate("Bar");

        var workSchedule = new WorkSchedule();

        workSchedule.Submit($"Parse {foo.Name}", [new Dependency(bar, SymbolFacts.SignatureKnown)], static _ => Task.Delay(10));
        workSchedule.Submit($"Parse {bar.Name}", [new Dependency(foo, SymbolFacts.SignatureKnown)], static _ => Task.Delay(10));

        await Assert.ThrowsAsync<CompilationStalledException>(() => workSchedule.Completion);
        Assert.AreEqual(0, workSchedule.Completed);
    }

    [TestMethod]
    public async Task TestScheduleAllowsPublishWithoutSubmit()
    {
        var symbolTable = new SymbolTable();
        var workSchedule = new WorkSchedule();
        var foo = symbolTable.GetOrCreate("Foo");
        workSchedule.Publish(foo, SymbolFacts.SignatureKnown);

        await workSchedule.Completion;
        Assert.AreEqual(0, workSchedule.Completed);
    }

    [TestMethod]
    public async Task TestScheduleClosesAfterAllComplete()
    {
        var symbolTable = new SymbolTable();
        var workSchedule = new WorkSchedule();

        workSchedule.Submit("Parse", [], static _ => Task.Delay(10));

        await Task.Delay(50);

        Assert.Throws<InvalidOperationException>(() => workSchedule.Submit("Parse", [], static _ => Task.Delay(10)));

        await workSchedule.Completion;
        Assert.AreEqual(1, workSchedule.Completed);
    }

    [TestMethod]
    public async Task TestScheduleAllowsMutuallyRecursiveJobs()
    {
        var symbolTable = new SymbolTable();
        var workSchedule = new WorkSchedule();

        workSchedule.Submit("Parse", [], async work =>
        {
            var foo = symbolTable.GetOrCreate("Foo");
            var bar = symbolTable.GetOrCreate("Bar");
            work.Publish(foo, SymbolFacts.SignatureKnown);
            work.Submit($"Typecheck {foo.Name}", [new Dependency(bar, SymbolFacts.SignatureKnown)], static _ => Task.Delay(10));

            await Task.Delay(10);
            work.Publish(bar, SymbolFacts.SignatureKnown);
            work.Submit($"Typecheck {bar.Name}", [new Dependency(foo, SymbolFacts.SignatureKnown)], static _ => Task.Delay(10));
        });

        await workSchedule.Completion;
        Assert.AreEqual(3, workSchedule.Completed);
    }
}
