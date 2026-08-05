using System.Threading.Tasks;

namespace Unity.GrantManager.Web.Middleware;

public record GitHubBlameInfo
{
    public string CommitSha { get; init; } = "";
    public string Author { get; init; } = "";
    public string Email { get; init; } = "";
    public string Message { get; init; } = "";
    public string? PullRequestUrl { get; init; }
    public int? PullRequestNumber { get; init; }
    public string? PullRequestTitle { get; init; }
}

public interface IBlameLookupService
{
    Task<GitHubBlameInfo?> GetBlameAsync(string repoPath, int line);
}
