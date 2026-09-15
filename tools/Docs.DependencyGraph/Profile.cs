using System.Diagnostics;

namespace Maq.Docs.DependencyGraph;

internal class Profile
{
    private readonly List<Entry> _entries = [];

    public T Measure<T>(string name, Func<T> action)
    {
        var started = Stopwatch.GetTimestamp();

        try
        {
            return action();
        }
        finally
        {
            AddEntry(name, Stopwatch.GetElapsedTime(started));
        }
    }

    public void Measure(string name, Action action)
    {
        var started = Stopwatch.GetTimestamp();

        try
        {
            action();
        }
        finally
        {
            AddEntry(name, Stopwatch.GetElapsedTime(started));
        }
    }

    public void Print(TextWriter writer)
    {
        if (_entries.Count == 0) return;

        var width = Math.Max("Total".Length, _entries.Max(entry => entry.Name.Length));

        foreach (var entry in _entries)
        {
            writer.WriteLine($"{entry.Name.PadRight(width)}  {Format(entry.Elapsed)}");
        }

        var total = TimeSpan.FromTicks(_entries.Sum(x => x.Elapsed.Ticks));

        writer.WriteLine($"{"Total".PadRight(width)}  {Format(total)}");
    }

    private static string Format(TimeSpan elapsed) => elapsed.TotalSeconds >= 1 ? $"{elapsed.TotalSeconds:N2} s" : $"{elapsed.TotalMilliseconds:N0} ms";

    private void AddEntry(string name, TimeSpan elapsed)
    {
        _entries.Add(new Entry(name, elapsed));
    }

    private record Entry(string Name, TimeSpan Elapsed);
}
