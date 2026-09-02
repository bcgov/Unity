using System;
using System.Collections.Generic;
using System.Linq;
using Volo.Abp;

namespace Unity.Notifications.EmailNotifications;

public enum EmailAttachmentValidationContext
{
    Email,
    Template
}

public sealed class MissingEmailAttachmentsException : UserFriendlyException
{
    public IReadOnlyList<string> FileNames { get; }

    public MissingEmailAttachmentsException(
        IEnumerable<string?> fileNames,
        EmailAttachmentValidationContext context)
        : this(NormalizeFileNames(fileNames), context)
    {
    }

    private MissingEmailAttachmentsException(
        IReadOnlyList<string> fileNames,
        EmailAttachmentValidationContext context)
        : base(BuildMessage(fileNames, context))
    {
        FileNames = fileNames;
    }

    private static IReadOnlyList<string> NormalizeFileNames(IEnumerable<string?> fileNames)
    {
        return fileNames
            .Select(fileName => string.IsNullOrWhiteSpace(fileName) ? "Unnamed attachment" : fileName.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(fileName => fileName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string BuildMessage(
        IReadOnlyList<string> fileNames,
        EmailAttachmentValidationContext context)
    {
        var files = string.Join(", ", fileNames);
        return context == EmailAttachmentValidationContext.Template
            ? $"This template contains attachments that cannot be found: {files}. Re-upload them in the email template before using it."
            : $"One or more email attachments cannot be found: {files}. Remove and re-upload them before sending.";
    }
}
