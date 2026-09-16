using System.CommandLine;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using Microsoft.Build.Locator;
using Maq.Tools.DependencyGraph;

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
    var profile = new Profile();

    var solutionPath = profile.Measure("Find solution", () => string.IsNullOrEmpty(solution) ? FindSolutionPath() : Path.GetFullPath(solution));

    if (!File.Exists(solutionPath))
        throw new FileNotFoundException($"Solution not found: {solutionPath}");

    var solutionDirectory = Path.GetDirectoryName(solutionPath) ?? throw new InvalidOperationException("Could not determine solution directory.");

    var document = profile.Measure("Generate document", () =>
    {
        var dependencyGraph = new DependencyGraph(solutionPath, solutionDirectory);

        return dependencyGraph.ToMarkdown();
    });

    if (string.IsNullOrEmpty(output))
    {
        profile.Measure("Write document", () => Console.Out.Write(document));
        Console.Out.WriteLine();
        profile.Print(Console.Error);
        return;
    }

    var outputPath = Path.IsPathRooted(output) ? output : Path.Combine(solutionDirectory, output);

    profile.Measure("Write document", () =>
    {
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? throw new InvalidOperationException("Could not get output directory path."));

        File.WriteAllText(outputPath, document, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    });

    var relativeOutputPath = Path
        .GetRelativePath(solutionDirectory, outputPath)
        .Replace('\\', '/');

    Console.Error.WriteLine($"Generated {relativeOutputPath}");
    Console.Error.WriteLine();

    profile.Print(Console.Error);
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
