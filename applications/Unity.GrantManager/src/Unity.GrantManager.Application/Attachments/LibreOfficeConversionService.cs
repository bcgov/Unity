using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace Unity.GrantManager.Attachments;

public class LibreOfficeConversionService : ILibreOfficeConversionService, ITransientDependency
{
    private static readonly Lazy<bool> IsInstalledCache = new(ProbeIsInstalled, true);

    private static string GetSafeFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("File name must be provided.", nameof(fileName));
        }

        var safeFileName = Path.GetFileName(fileName);
        if (safeFileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            throw new ArgumentException("File name contains invalid characters.", nameof(fileName));
        }

        return safeFileName;
    }

    private static bool ProbeIsInstalled()
    {
        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "libreoffice",
                    Arguments = "--version",
                    UseShellExecute = false
                }
            };

            process.Start();
            var exited = process.WaitForExit(5000);

            return exited && process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    public bool IsInstalled()
    {
        return IsInstalledCache.Value;
    }

    public async Task<byte[]> ConvertToPdfAsync(byte[] fileContent, string fileName)
    {
        var safeFileName = GetSafeFileName(fileName);
        var tempDir = Path.Combine(Path.GetTempPath(), "unity-libreoffice-preview", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            var inputPath = Path.Combine(tempDir, safeFileName);
            await File.WriteAllBytesAsync(inputPath, fileContent);

            var startInfo = new ProcessStartInfo
            {
                FileName = "libreoffice",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };
            startInfo.ArgumentList.Add("--headless");
            startInfo.ArgumentList.Add("--convert-to");
            startInfo.ArgumentList.Add("pdf");
            startInfo.ArgumentList.Add("--outdir");
            startInfo.ArgumentList.Add(tempDir);
            startInfo.ArgumentList.Add(inputPath);

            using var process = new Process
            {
                StartInfo = startInfo
            };

            process.Start();

            var standardOutputTask = process.StandardOutput.ReadToEndAsync();
            var standardErrorTask = process.StandardError.ReadToEndAsync();
            var exitTask = process.WaitForExitAsync();
            var completedTask = await Task.WhenAny(exitTask, Task.Delay(60000));

            if (completedTask != exitTask)
            {
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync();
                await Task.WhenAll(standardOutputTask, standardErrorTask);
                throw new InvalidOperationException($"LibreOffice conversion timed out for file: {safeFileName}");
            }

            await exitTask;
            await Task.WhenAll(standardOutputTask, standardErrorTask);

            if (process.ExitCode != 0)
            {
                var error = await standardErrorTask;
                throw new InvalidOperationException($"LibreOffice conversion failed for file: {safeFileName}. Error: {error}");
            }

            var pdfFileName = Path.GetFileNameWithoutExtension(safeFileName) + ".pdf";
            var pdfPath = Path.Combine(tempDir, pdfFileName);

            if (!File.Exists(pdfPath))
            {
                throw new InvalidOperationException($"LibreOffice did not produce a PDF output for file: {safeFileName}");
            }

            return await File.ReadAllBytesAsync(pdfPath);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }
}
