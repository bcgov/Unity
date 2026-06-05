using System;
using System.Diagnostics;
using System.Collections.Generic;
using Unity.Notifications.TeamsNotifications;

namespace Unity.GrantManager.Web.Middleware
{
    internal static class ExceptionNotificationHelpers
    {
        public static string NormalizeRepoPath(string fullPath)
        {
            if (string.IsNullOrWhiteSpace(fullPath)) return fullPath;

            // Normalize separators for comparison
            var path = fullPath.Replace("\\", "/");

            // Prefer repository-relative path under applications/Unity.GrantManager/src/
            const string repoMarker = "applications/unity.grantmanager/src/";
            int idx = path.IndexOf(repoMarker, StringComparison.OrdinalIgnoreCase);
            if (idx >= 0)
            {
                return path[(idx + repoMarker.Length)..].TrimStart('/');
            }

            // Fallback to any src/ directory
            const string srcMarker = "src/";
            idx = path.IndexOf(srcMarker, StringComparison.OrdinalIgnoreCase);
            if (idx >= 0)
            {
                return path[(idx + srcMarker.Length)..].TrimStart('/');
            }

            // Last resort: strip drive letter (Windows) and return the path relative to repository root if possible
            // If we can't determine a repo-relative path, return just the file name so notifications remain readable
            try
            {
                return System.IO.Path.GetFileName(path);
            }
            catch
            {
                return path;
            }
        }

        public static (string? File, int? Line)? GetTopFrame(Exception ex)
        {
            var trace = new StackTrace(ex, true);

            foreach (var frame in trace.GetFrames() ?? Array.Empty<StackFrame>())
            {
                var file = frame.GetFileName();
                var line = frame.GetFileLineNumber();

                if (!string.IsNullOrWhiteSpace(file) && line > 0)
                {
                    return (file, line);
                }
            }

            return null;
        }

        public static string BuildBlamePath(string sourceFile)
        {
            if (string.IsNullOrWhiteSpace(sourceFile)) return sourceFile;

            // Normalize separators
            var path = sourceFile.Replace("\\", "/").TrimStart('/');

            // If caller already passed a repo-rooted path, return as-is
            string result;
            if (path.StartsWith("applications/", StringComparison.OrdinalIgnoreCase))
                result = path;
            else if (path.StartsWith("src/", StringComparison.OrdinalIgnoreCase))
                result = $"applications/Unity.GrantManager/{path}";
            else
                // Default: assume sourceFile is the portion after src/, so include src/
                result = $"applications/Unity.GrantManager/src/{path}";

            return result;
        }

        public static string BuildApplicationStackExcerpt(Exception ex)
        {
            var trace = new StackTrace(ex, true);

            var frames = trace.GetFrames();

            if (frames == null || frames.Length == 0)
            {
                return "(no stack trace)";
            }

            // Keep only application frames
            var appFrames = new System.Collections.Generic.List<StackFrame>();

            foreach (var f in frames)
            {
                var typeName = f.GetMethod()?.DeclaringType?.FullName;

                if (string.IsNullOrWhiteSpace(typeName))
                    continue;

                if (typeName.StartsWith("Unity.", StringComparison.Ordinal))
                {
                    appFrames.Add(f);
                    if (appFrames.Count >= 5)
                        break;
                }
            }

            if (appFrames.Count == 0)
                return ex.Message;

            var lines = new System.Collections.Generic.List<string>();
            for (int i = 0; i < appFrames.Count; i++)
            {
                var f = appFrames[i];
                var method = f.GetMethod();
                var className = method?.DeclaringType?.Name ?? "UnknownClass";
                var methodName = method?.Name ?? "UnknownMethod";
                var file = f.GetFileName();
                var fileName = string.IsNullOrWhiteSpace(file) ? "unknown" : System.IO.Path.GetFileName(file);
                var line = f.GetFileLineNumber();
                lines.Add($"{i + 1}. {className}.{methodName}() in {fileName}:{line}");
            }

            return string.Join(Environment.NewLine, lines);
        }

        public static List<Fact> BuildFacts(
            string exTypeName,
            string exMessage,
            string endpoint,
            string userName,
            string tenantName,
            string stackTrace,
            string sourceFile,
            int? sourceLine,
            string releaseNumber,
            string? innerMessage = null)
        {
            var facts = new List<Fact>
            {
                new() { Name = "Exception", Value = exTypeName },
                new() { Name = "Message", Value = exMessage },
                new() { Name = "Endpoint", Value = endpoint },
                new() { Name = "User", Value = userName },
                new() { Name = "Tenant", Value = tenantName },
                new() { Name = "Stack Trace", Value = stackTrace },
                new() { Name = "Source", Value = sourceLine.HasValue ? $"{sourceFile}:{sourceLine}" : sourceFile },
                new() { Name = "Release Number", Value = releaseNumber },
            };

            if (!string.IsNullOrEmpty(innerMessage))
            {
                facts.Add(new Fact { Name = "Inner Exception", Value = innerMessage });
            }

            return facts;
        }
    }
}
