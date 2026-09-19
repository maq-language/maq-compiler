using System.Diagnostics;
using System.Threading.Tasks.Dataflow;

namespace Maq.Compiler;

public class WorkSchedule : IPublisher
{
    private readonly record struct Publication(Symbol Symbol, SymbolFacts Facts);

    private readonly BufferBlock<WorkItem> _submitted = new();
    private readonly BufferBlock<Publication> _publications = new();
    private readonly BufferBlock<WorkItem> _ready = new();
    private readonly Queue<WorkItem> _waiting = new();

    private readonly ActionBlock<byte> _synchronization;
    private readonly ActionBlock<WorkItem> _execution;

    private int _outstanding;
    private int _pendingCompletions;
    private int _completed;
    private int _synchronizationWaiting;

    public WorkSchedule()
    {
        _synchronization = new ActionBlock<byte>(
            _ => Synchronize());

        _execution = new ActionBlock<WorkItem>(
            ExecuteAsync,
            new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount,
                EnsureOrdered = false
            });

        _ready.LinkTo(
            _execution,
            new DataflowLinkOptions
            {
                PropagateCompletion = true
            });
    }

    public Task Completion => _execution.Completion;

    public int Completed => Volatile.Read(ref _completed);

    public void Submit(WorkItem work)
    {
        if (!_submitted.Post(work))
        {
            throw new InvalidOperationException($"Could not submit work: {work}");
        }

        EnqueueSynchronize();
    }

    public void Submit(string name, IEnumerable<Dependency> dependencies, WorkItemAction action)
    {
        Submit(new WorkItem(name, dependencies, action));
    }

    public void Publish(Symbol symbol, SymbolFacts facts)
    {
        if (!_publications.Post(new Publication(symbol, facts)))
        {
            throw new InvalidOperationException($"Could not publish facts for: {symbol}");
        }

        EnqueueSynchronize();
    }

    private void EnqueueSynchronize()
    {
        if (Interlocked.Exchange(ref _synchronizationWaiting, 1) == 0 && !_synchronization.Post(0))
        {
            throw new InvalidOperationException("The work schedule is no longer accepting updates.");
        }
    }

    private void Synchronize()
    {
        Interlocked.Exchange(ref _synchronizationWaiting, 0);

        // NOTE(alex): Drain submission queue.
        while (_submitted.TryReceive(out var work))
        {
            _outstanding += 1;

            if (work.IsReady)
            {
                _ready.Post(work);
                continue;
            }

            _waiting.Enqueue(work);
        }

        // NOTE(alex): Drain publication queue.
        var changed = false;

        while (_publications.TryReceive(out var publication))
        {
            changed |= publication.Symbol.Publish(publication.Facts);
        }

        // NOTE(alex): Release ready work.
        if (changed)
        {
            var count = _waiting.Count;

            for (var i = 0; i < count; i += 1)
            {
                var work = _waiting.Dequeue();

                if (!work.IsReady)
                {
                    _waiting.Enqueue(work);
                    continue;
                }

                _ready.Post(work);
            }
        }

        var completed = Interlocked.Exchange(ref _pendingCompletions, 0);
        _completed += completed;
        _outstanding -= completed;

        // NOTE(alex): A follow-up pass may contain changes that arrived after their input was drained.
        if (Volatile.Read(ref _synchronizationWaiting) != 0)
            return;

        if (_outstanding == 0)
        {
            _ready.Complete();
            _synchronization.Complete();
            return;
        }

        if (_waiting.Count == _outstanding)
        {
            ((IDataflowBlock)_execution).Fault(new CompilationStalledException());
            _synchronization.Complete();
        }
    }

    private async Task ExecuteAsync(WorkItem work)
    {
        try
        {
            await work.Run(this).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            throw new WorkItemException(work, exception);
        }
        finally
        {
            Interlocked.Increment(ref _pendingCompletions);
            EnqueueSynchronize();
        }
    }
}
