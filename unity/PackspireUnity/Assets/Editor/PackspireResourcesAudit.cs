using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Packspire.Editor
{
    internal static class PackspireResourcesAudit
    {
        private const string ResourcesRoot = "Assets/Resources";
        private const string RuntimeScriptsRoot = "Assets/Scripts";
        private const string ReportPath = "../../docs/performance/RESOURCES_USAGE_REPORT.md";
        private static readonly Regex ResourcePathPattern = new Regex(
            "(?:PackspireResources|Resources)\\.(?:Load|LoadAll)(?:<[^>]+>)?\\(\\s*\"([^\"\\r\\n]+)\"",
            RegexOptions.Compiled);

        [MenuItem("Tools/Packspire/Audit/Write Resources usage report")]
        private static void WriteReport()
        {
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            if (string.IsNullOrEmpty(projectRoot)) return;
            string resourcesPath = Path.Combine(projectRoot, ResourcesRoot);
            string scriptsPath = Path.Combine(projectRoot, RuntimeScriptsRoot);
            string reportPath = Path.GetFullPath(Path.Combine(projectRoot, ReportPath));

            var files = Directory.Exists(resourcesPath)
                ? Directory.EnumerateFiles(resourcesPath, "*", SearchOption.AllDirectories)
                    .Where(path => !path.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                    .Select(path => new FileInfo(path))
                    .OrderByDescending(file => file.Length)
                    .ToList()
                : new List<FileInfo>();
            HashSet<string> literalPaths = FindLiteralResourcePaths(scriptsPath);

            Directory.CreateDirectory(Path.GetDirectoryName(reportPath) ?? projectRoot);
            File.WriteAllText(reportPath, BuildReport(files, literalPaths, resourcesPath), Encoding.UTF8);
            AssetDatabase.Refresh();
            Debug.Log($"Resources audit written to {reportPath}");
        }

        private static HashSet<string> FindLiteralResourcePaths(string scriptsPath)
        {
            var result = new HashSet<string>(StringComparer.Ordinal);
            if (!Directory.Exists(scriptsPath)) return result;
            foreach (string sourcePath in Directory.EnumerateFiles(scriptsPath, "*.cs", SearchOption.AllDirectories))
            {
                string source = File.ReadAllText(sourcePath);
                foreach (Match match in ResourcePathPattern.Matches(source)) result.Add(match.Groups[1].Value);
            }
            return result;
        }

        private static string BuildReport(List<FileInfo> files, HashSet<string> literalPaths, string resourcesPath)
        {
            long totalBytes = files.Sum(file => file.Length);
            var builder = new StringBuilder();
            builder.AppendLine("# Resources usage report");
            builder.AppendLine();
            builder.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            builder.AppendLine();
            builder.AppendLine($"- Files: {files.Count}");
            builder.AppendLine($"- Disk size: {FormatBytes(totalBytes)}");
            builder.AppendLine($"- Literal load paths found in runtime C#: {literalPaths.Count}");
            builder.AppendLine();
            builder.AppendLine("This report is an inventory, not an automatic deletion list. Dynamic paths, UXML/USS URLs, ScriptableObject references, and scene references must be checked before removal.");
            builder.AppendLine();
            builder.AppendLine("## Largest files");
            builder.AppendLine();
            builder.AppendLine("| File | Size |");
            builder.AppendLine("|---|---:|");
            foreach (FileInfo file in files.Take(50))
            {
                string relative = Path.GetRelativePath(resourcesPath, file.FullName).Replace('\\', '/');
                builder.AppendLine($"| `{relative}` | {FormatBytes(file.Length)} |");
            }
            builder.AppendLine();
            builder.AppendLine("## Literal runtime load paths");
            builder.AppendLine();
            foreach (string path in literalPaths.OrderBy(path => path, StringComparer.Ordinal))
                builder.AppendLine($"- `{path}`");
            return builder.ToString();
        }

        private static string FormatBytes(long value)
        {
            if (value >= 1024L * 1024L) return $"{value / (1024d * 1024d):0.0} MB";
            if (value >= 1024L) return $"{value / 1024d:0.0} KB";
            return $"{value} B";
        }
    }
}
