using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace Unity.GrantManager.Attachments;

public class LibreOfficeConversionService : ILibreOfficeConversionService, ITransientDependency
{
    public bool IsInstalled()
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "libreoffice",
                    Arguments = "--version",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false
                }
            };
            process.Start();
            process.WaitForExit(5000);
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    public async Task<byte[]> ConvertToPdfAsync(byte[] fileContent, string fileName)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "unity-libreoffice-preview", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            var inputPath = Path.Combine(tempDir, fileName);
            await File.WriteAllBytesAsync(inputPath, fileContent);

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "libreoffice",
                    Arguments = $"--headless --convert-to pdf --outdir \"{tempDir}\" \"{inputPath}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false
                }
            };

            process.Start();
            var timedOut = !process.WaitForExit(60000);

            if (timedOut)
            {
                process.Kill();
                throw new InvalidOperationException($"LibreOffice conversion timed out for file: {fileName}");
            }

            if (process.ExitCode != 0)
            {
                var error = await process.StandardError.ReadToEndAsync();
                throw new InvalidOperationException($"LibreOffice conversion failed for file: {fileName}. Error: {error}");
            }

            var pdfFileName = Path.GetFileNameWithoutExtension(fileName) + ".pdf";
            var pdfPath = Path.Combine(tempDir, pdfFileName);

            if (!File.Exists(pdfPath))
            {
                throw new InvalidOperationException($"LibreOffice did not produce a PDF output for file: {fileName}");
            }

            return await File.ReadAllBytesAsync(pdfPath);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }
}
