using System.CommandLine;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using Microsoft.Build.Locator;
using Maq.Docs.DependencyGraph;

var solutionArgument = new Argument<string?>("solution")
{
    Description = "Optional path to the solution file.",
    Arity = ArgumentArity.ZeroOrOne
};

var outputOption = new Option<string>("--output", "-o")
{
    Description = "Output path relative to the solution directory."
};

var rootCommand = new RootCommand("Generates project dependency documentation.")
{
    solutionArgument,
    outputOption
};

rootCommand.SetAction(parseResult =>
{
    MSBuildLocator.RegisterDefaults();

    var solution = parseResult.GetValue(solutionArgument);
    var output = parseResult.GetValue(outputOption);

    Run(solution, output);
});

return rootCommand.Parse(args).Invoke();

static void Run(string? solution, string? output)
{
    var solutionPath = string.IsNullOrEmpty(solution) ? FindSolutionPath() : Path.GetFullPath(solution);

    if (!File.Exists(solutionPath))
        throw new FileNotFoundException($"Solution not found: {solutionPath}");

    var solutionDirectory = Path.GetDirectoryName(solutionPath) ?? throw new InvalidOperationException("Could not determine solution directory.");

    var stopwatch = Stopwatch.StartNew();

    var dependencyGraph = new DependencyGraph(solutionPath, solutionDirectory);

    if (string.IsNullOrEmpty(output))
    {
        Console.Out.Write(dependencyGraph.ToMarkdown());
        return;
    }

    var outputPath = Path.IsPathRooted(output) ? output : Path.Combine(solutionDirectory, output);

    Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? throw new InvalidOperationException("Could not get output directory path."));

    File.WriteAllText(outputPath, dependencyGraph.ToMarkdown(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

    stopwatch.Stop();

    var relativeOutputPath = Path
        .GetRelativePath(solutionDirectory, outputPath)
        .Replace('\\', '/');

    Console.Error.WriteLine($"Generated {relativeOutputPath} in {stopwatch.Elapsed.TotalMilliseconds:N0} ms");
}

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
