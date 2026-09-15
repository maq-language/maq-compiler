using System.Security.Cryptography;
using System.Text;
using Microsoft.Build.Construction;
using Microsoft.Build.Graph;
using Microsoft.Build.Locator;

namespace Maq.Docs.DependencyGraph;

internal class DependencyGraph
{
    private readonly IReadOnlyCollection<ProjectInfo> _projects;
    private readonly IReadOnlyDictionary<string, ProjectInfo> _projectByPath;
    private readonly IReadOnlyCollection<ProjectEdge> _edges;
    private readonly string _solutionPath;

    public DependencyGraph(string solutionPath, string solutionDirectory)
    {
        var solution = SolutionFile.Parse(solutionPath);

        var entryProjectPaths = solution.ProjectsInOrder
            .Where(project => project.ProjectType != SolutionProjectType.SolutionFolder)
            .Select(project => project.AbsolutePath)
            .Where(path => path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
            .Where(File.Exists)
            .Select(Path.GetFullPath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (entryProjectPaths.Length == 0)
            throw new InvalidOperationException($"No projects were found in solution '{solutionPath}'.");

        var entryPoints = entryProjectPaths
            .Select(path => new ProjectGraphEntryPoint(path))
            .ToArray();

        var graph = new ProjectGraph(entryPoints);

        // NOTE(alex): A ProjectGraph can contain multiple nodes for the same physical project
        // when global properties differ. For documentation purposes we collapse those into one
        // project node and take the union of the project-reference edges.

        var projectPaths = graph.ProjectNodes
            .Select(node => NormalizePath(node.ProjectInstance.FullPath))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(path => GetRelativePath(solutionDirectory, path), StringComparer.OrdinalIgnoreCase)
            .ToArray();

        _edges = CollectEdges(graph);

        _projects = projectPaths
            .Select(path => new ProjectInfo(path, GetRelativePath(solutionDirectory, path), Path.GetFileNameWithoutExtension(path)))
            .ToArray();

        var duplicateNames = _projects
            .GroupBy(project => project.Name, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();

        if (duplicateNames.Length != 0)
            throw new InvalidOperationException($"Duplicate project names: {string.Join(", ", duplicateNames)}");

        _projectByPath = _projects.ToDictionary(project => project.FullPath, StringComparer.OrdinalIgnoreCase);
        _solutionPath = solutionPath;
    }

    public string ToMarkdown()
    {
        var builder = new StringBuilder();

        builder.AppendLine("---");
        builder.AppendLine("title: Project dependencies");
        builder.AppendLine("---");
        builder.AppendLine();
        builder.AppendLine("# Project dependencies");
        builder.AppendLine();

        builder.AppendLine("> [!NOTE]");
        builder.AppendLine("> This page is generated automatically from the MSBuild project graph. Do not edit it manually.");
        builder.AppendLine();

        builder.AppendLine($"Source solution: `{EscapeMarkdown(Path.GetFileName(_solutionPath))}`");
        builder.AppendLine();

        builder.AppendLine($"**Projects**: {_projects.Count}");
        builder.AppendLine($"**Project references**: {_edges.Count}");
        builder.AppendLine();

        builder.AppendLine("```mermaid");
        builder.AppendLine("flowchart LR");

        // NOTE(alex): Declare every node explicitly. This ensures projects with no
        // ProjectReference items still appear in the diagram.

        foreach (var project in _projects)
        {
            builder.Append("    ");
            builder.Append(GetNodeId(project.RelativePath));
            builder.Append("[\"");
            builder.Append(EscapeMermaid(project.Name));
            builder.AppendLine("\"]");
        }

        if (_edges.Count > 0)
        {
            builder.AppendLine();

            foreach (var edge in _edges)
            {
                if (_projectByPath.TryGetValue(edge.From, out var fromProject) && _projectByPath.TryGetValue(edge.To, out var toProject))
                {
                    builder.Append("    ");
                    builder.Append(GetNodeId(fromProject.RelativePath));
                    builder.Append(" --> ");
                    builder.AppendLine(GetNodeId(toProject.RelativePath));
                }
            }
        }

        builder.AppendLine("```");
        builder.AppendLine();

        builder.AppendLine("## Projects");
        builder.AppendLine();

        builder.AppendLine("| Project | Path | Dependencies |");
        builder.AppendLine("| --- | --- | ---: |");

        foreach (var project in _projects)
        {
            var dependencyCount = _edges.Count(edge => string.Equals(edge.From, project.FullPath, StringComparison.OrdinalIgnoreCase));

            builder.Append("| ");
            builder.Append(EscapeMarkdown(project.Name));
            builder.Append(" | `");
            builder.Append(EscapeMarkdown(project.RelativePath));
            builder.Append("` | ");
            builder.Append(dependencyCount);
            builder.AppendLine(" |");
        }

        return builder.ToString();
    }

    private static IReadOnlyCollection<ProjectEdge> CollectEdges(ProjectGraph graph)
    {
        var edges = new List<ProjectEdge>();

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var node in graph.ProjectNodes)
        {
            var fromPath = NormalizePath(node.ProjectInstance.FullPath);

            foreach (var reference in node.ProjectReferences)
            {
                var toPath = NormalizePath(reference.ProjectInstance.FullPath);

                var key = $"{fromPath}\0{toPath}";

                if (!seen.Add(key)) continue;

                edges.Add(new ProjectEdge(fromPath, toPath));
            }
        }

        return edges
            .OrderBy(edge => edge.From, StringComparer.OrdinalIgnoreCase)
            .ThenBy(edge => edge.To, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string EscapeMermaid(string value)
    {
        return value
            .Replace("&", "&amp;")
            .Replace("\"", "&quot;");
    }

    private static string EscapeMarkdown(string value)
    {
        return value
            .Replace("|", "\\|")
            .Replace("`", "\\`");
    }

    private static string GetNodeId(string relativePath)
    {
        var normalized = relativePath
            .Replace('\\', '/')
            .ToLowerInvariant();

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));

        return "p_" + Convert.ToHexString(hash).Substring(0, 12).ToLowerInvariant();
    }

    private static string GetRelativePath(string solutionDirectory, string projectPath)
    {
        return Path
            .GetRelativePath(solutionDirectory, projectPath)
            .Replace('\\', '/');
    }

    private static string NormalizePath(string path) => Path.GetFullPath(path);
}
