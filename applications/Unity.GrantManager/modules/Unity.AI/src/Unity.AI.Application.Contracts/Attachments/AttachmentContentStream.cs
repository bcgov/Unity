using System;
using System.IO;
using System.Threading.Tasks;

namespace Unity.AI.Attachments;

public sealed class AttachmentContentStream(Stream content, string contentType) : IDisposable, IAsyncDisposable
{
    public Stream Content { get; } = content ?? throw new ArgumentNullException(nameof(content));

    public string ContentType { get; } =
        string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType;

    public static AttachmentContentStream Empty { get; } =
        new(Stream.Null, "application/octet-stream");

    public void Dispose() => Content.Dispose();

    public ValueTask DisposeAsync() => Content.DisposeAsync();
}
