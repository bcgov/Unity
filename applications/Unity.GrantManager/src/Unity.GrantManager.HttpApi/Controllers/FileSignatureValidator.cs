using System;
using System.Collections.Generic;
using System.Linq;

namespace Unity.GrantManager.Controllers
{
    internal static class FileSignatureValidator
    {
        private const int PdfHeaderScanWindow = 1024;

        private static readonly byte[] Pdf = [0x25, 0x50, 0x44, 0x46];
        private static readonly byte[] ZipLocal = [0x50, 0x4B, 0x03, 0x04];
        private static readonly byte[] ZipEmpty = [0x50, 0x4B, 0x05, 0x06];
        private static readonly byte[] ZipSpanned = [0x50, 0x4B, 0x07, 0x08];
        private static readonly byte[] Ole = [0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1];
        private static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF];
        private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
        private static readonly byte[] Gif = [0x47, 0x49, 0x46, 0x38];

        private static readonly Dictionary<string, byte[][]> SignaturesByExtension = new()
        {
            ["docx"] = [ZipLocal, ZipEmpty, ZipSpanned],
            ["xlsx"] = [ZipLocal, ZipEmpty, ZipSpanned],
            ["pptx"] = [ZipLocal, ZipEmpty, ZipSpanned],
            ["zip"] = [ZipLocal, ZipEmpty, ZipSpanned],
            ["doc"] = [Ole],
            ["xls"] = [Ole],
            ["ppt"] = [Ole],
            ["jpg"] = [Jpeg],
            ["jpeg"] = [Jpeg],
            ["png"] = [Png],
            ["gif"] = [Gif],
        };

        public static bool HasValidSignature(string extension, byte[] content)
        {
            if (content.Length == 0)
            {
                return true;
            }

            var normalizedExtension = extension.ToLowerInvariant();

            // ISO 32000 permits the "%PDF-" header to appear anywhere within the first
            // 1024 bytes (e.g. a leading BOM or stray bytes some tools prepend), rather
            // than requiring it at offset 0 like the other binary formats below.
            if (normalizedExtension == "pdf")
            {
                return ContainsSignatureWithinWindow(content, Pdf, PdfHeaderScanWindow);
            }

            if (!SignaturesByExtension.TryGetValue(normalizedExtension, out var signatures))
            {
                return true;
            }

            return signatures.Any(signature =>
                content.Length >= signature.Length &&
                content.Take(signature.Length).SequenceEqual(signature));
        }

        private static bool ContainsSignatureWithinWindow(byte[] content, byte[] signature, int window)
        {
            var scanLength = Math.Min(content.Length, window);
            for (var offset = 0; offset <= scanLength - signature.Length; offset++)
            {
                if (content.Skip(offset).Take(signature.Length).SequenceEqual(signature))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
