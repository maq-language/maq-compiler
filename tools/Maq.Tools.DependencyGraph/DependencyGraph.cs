using System.Security.Cryptography;
using System.Text;
using Microsoft.Build.Construction;
using Microsoft.Build.Graph;
using Microsoft.Build.Locator;

namespace Maq.Tools.DependencyGraph;

internal class DependencyGraph
{
    private static readonly StringComparer PathComparer =
        OperatingSystem.IsWindows()
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;

    private static readonly StringComparison PathComparison =
        OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

    private readonly IReadOnlyCollection<ProjectInfo> _projects;
    private readonly IReadOnlyDictionary<string, ProjectInfo> _projectByPath;
    private readonly IReadOnlyCollection<ProjectEdge> _edges;
    private readonly string _solutionPath;

    public DependencyGraph(string solutionPath, string solutionDirectory)
    {
        _solutionPath = solutionPath;

        var solution = SolutionFile.Parse(solutionPath);
        var entryProjectPaths = GetEntryProjectPaths(solution);

        if (entryProjectPaths.Count == 0)
        {
            _projects = [];
            _projectByPath = new Dictionary<string, ProjectInfo>(PathComparer);
            _edges = [];
            return;
        }

        var graph = CreateProjectGraph(entryProjectPaths);

        _projects = GetProjects(graph, solutionDirectory);
        _edges = CollectEdges(graph);

        // NOTE(alex): Project file names are not required to be unique, so we use the full path as the key.
        _projectByPath = _projects.ToDictionary(project => project.FullPath, PathComparer);
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

        AppendDependencyGraph(builder);
        AppendProjectTable(builder);

        return builder.ToString();
    }

    private static IReadOnlyCollection<string> GetEntryProjectPaths(SolutionFile solution)
    {
        return solution.ProjectsInOrder
            .Select(project => project.AbsolutePath)
            .Where(path => path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
            .Where(File.Exists)
            .Select(Path.GetFullPath)
            .Distinct(PathComparer)
            .ToArray();
    }

    private static ProjectGraph CreateProjectGraph(IEnumerable<string> projectPaths) => new ProjectGraph(projectPaths.Select(path => new ProjectGraphEntryPoint(path)));

    private static IReadOnlyCollection<ProjectInfo> GetProjects(ProjectGraph graph, string solutionDirectory)
    {
        // NOTE(alex): ProjectGraph may contain multiple evaluations of the same project file
        // with different global properties. Collapse them by project path and union their
        // project-reference edges for documentation purposes.
        return graph.ProjectNodes
            .Select(node => Path.GetFullPath(node.ProjectInstance.FullPath))
            .Distinct(PathComparer)
            .Select(path => new ProjectInfo(path, GetRelativePath(solutionDirectory, path), Path.GetFileNameWithoutExtension(path)))
            .OrderBy(project => project.RelativePath, PathComparer)
            .ToArray();
    }

    private static IReadOnlyCollection<ProjectEdge> CollectEdges(ProjectGraph graph)
    {
        var edges = new List<ProjectEdge>();
        var seen = new HashSet<string>(PathComparer);

        foreach (var node in graph.ProjectNodes)
        {
            var fromPath = Path.GetFullPath(node.ProjectInstance.FullPath);

            foreach (var reference in node.ProjectReferences)
            {
                var toPath = Path.GetFullPath(reference.ProjectInstance.FullPath);

                var key = $"{fromPath}\0{toPath}"; // NOTE(alex): Join paths with \0 separator.

                if (!seen.Add(key))
                    continue;

                edges.Add(new ProjectEdge(fromPath, toPath));
            }
        }

        return edges
            .OrderBy(edge => edge.From, PathComparer)
            .ThenBy(edge => edge.To, PathComparer)
            .ToArray();
    }

    private void AppendDependencyGraph(StringBuilder builder)
    {
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
                if (!_projectByPath.TryGetValue(edge.From, out var fromProject) ||
                    !_projectByPath.TryGetValue(edge.To, out var toProject))
                {
                    continue;
                }

                builder.Append("    ");
                builder.Append(GetNodeId(fromProject.RelativePath));
                builder.Append(" --> ");
                builder.AppendLine(GetNodeId(toProject.RelativePath));
            }
        }

        builder.AppendLine("```");
        builder.AppendLine();
    }

    private void AppendProjectTable(StringBuilder builder)
    {
        builder.AppendLine("## Projects");
        builder.AppendLine();

        builder.AppendLine("| Project | Path | Dependencies |");
        builder.AppendLine("| --- | --- | ---: |");

        foreach (var project in _projects)
        {
            var dependencyCount = _edges.Count(edge =>
                string.Equals(edge.From, project.FullPath, PathComparison));

            builder.Append("| ");
            builder.Append(EscapeMarkdown(project.Name));
            builder.Append(" | `");
            builder.Append(EscapeMarkdown(project.RelativePath));
            builder.Append("` | ");
            builder.Append(dependencyCount);
            builder.AppendLine(" |");
        }
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
}
