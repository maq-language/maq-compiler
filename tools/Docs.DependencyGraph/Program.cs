using System.Diagnostics;
using System.Reflection;
using System.Text;
using Microsoft.Build.Locator;
using Maq.Docs.DependencyGraph;

if (args.Length < 1 || args.Length > 2)
{
    Console.Error.WriteLine(
        """
        Usage:
          Docs.DependencyGraph <SOLUTION> [<OUTPUT>]

        Examples:
          Docs.DependencyGraph Project.sln
          Docs.DependencyGraph Project.sln docs/architecture/project-dependencies.md
        """
    );
    return;
}

MSBuildLocator.RegisterDefaults();

var solutionPath = Path.GetFullPath(args[0]);

if (!File.Exists(solutionPath))
{
    throw new FileNotFoundException($"Solution does not exist: {solutionPath}");
}

var solutionDirectory = Path.GetDirectoryName(solutionPath) ?? throw new InvalidOperationException("Could not determine solution directory.");

var stopwatch = Stopwatch.StartNew();

var dependencyGraph = new DependencyGraph(solutionPath, solutionDirectory);

if (args.Length == 2)
{
    var outputPath = Path.GetFullPath(args[1]);

    var outputDirectory = Path.GetDirectoryName(outputPath);

    if (!string.IsNullOrWhiteSpace(outputDirectory))
    {
        Directory.CreateDirectory(outputDirectory);
    }

    File.WriteAllText(outputPath, dependencyGraph.ToMarkdown(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

    stopwatch.Stop();

    var relativeOutputPath = Path
        .GetRelativePath(solutionDirectory, outputPath)
        .Replace('\\', '/');

    Console.WriteLine($"Generated {relativeOutputPath} in {stopwatch.Elapsed.TotalMilliseconds:N0} ms");

    return;
}

Console.Write(dependencyGraph.ToMarkdown());
return;

static string FindSolutionPath()
{
    var assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? throw new InvalidOperationException("Could not determine assembly location.");

    var directory = new DirectoryInfo(assemblyPath);

    while (directory != null)
    {
        var solutions = directory
            .EnumerateFiles("*", SearchOption.TopDirectoryOnly)
            .Where(file => file.Extension.Equals(".sln", StringComparison.OrdinalIgnoreCase) || file.Extension.Equals(".slnx", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (solutions.Length == 1)
            return solutions[0].FullName;

        if (solutions.Length > 1)
            throw new InvalidOperationException($"Multiple solution files found in '{directory.FullName}'.");

        directory = directory.Parent;
    }

    throw new FileNotFoundException("Could not find a .sln or .slnx file.");
}
