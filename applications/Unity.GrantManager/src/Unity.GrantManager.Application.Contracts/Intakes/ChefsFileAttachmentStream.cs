using System;
using System.IO;
using System.Threading.Tasks;

namespace Unity.GrantManager.Intakes;

/// <summary>
/// Stream of a CHEFS file attachment plus its content type.
/// The Content stream owns its underlying temp file; dispose to release.
/// </summary>
public sealed class ChefsFileAttachmentStream : IDisposable, IAsyncDisposable
{
    public Stream Content { get; }
    public string ContentType { get; }

    public ChefsFileAttachmentStream(Stream content, string contentType)
    {
        Content = content ?? throw new ArgumentNullException(nameof(content));
        ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType;
    }

    public static ChefsFileAttachmentStream Empty { get; } =
        new(Stream.Null, "application/octet-stream");

    public void Dispose() => Content.Dispose();

    public ValueTask DisposeAsync() => Content.DisposeAsync();
}
