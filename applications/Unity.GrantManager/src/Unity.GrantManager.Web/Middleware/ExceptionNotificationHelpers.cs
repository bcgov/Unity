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
            const string marker = "src/";

            int idx = fullPath.IndexOf(marker, StringComparison.OrdinalIgnoreCase);

            if (idx < 0)
                return fullPath.Replace("\\", "/");

            return fullPath[(idx + marker.Length)..]
                .Replace("\\", "/");
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
            return $"applications/Unity.GrantManager/{sourceFile}";
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
